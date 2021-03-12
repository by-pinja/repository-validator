﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using ValidationLibrary;
using ValidationLibrary.Csv;
using ValidationLibrary.GitHub;
using ValidationLibrary.Rules;
using ValidationLibrary.Slack;
using ValidationLibrary.Utils;

namespace Runner
{
    public class Program
    {
        private static readonly GitHubReportConfig _gitHubReporterConfig = new GitHubReportConfig
        {
            Prefix = "[Automatic validation]",
            GenericNotice =
                "These issues are created, closed and reopened by [repository validator](https://github.com/by-pinja/repository-validator) when commits are pushed to repository. " + Environment.NewLine +
                Environment.NewLine +
                "If there are problems, please add an issue to [repository validator](https://github.com/by-pinja/repository-validator)" + Environment.NewLine +
                Environment.NewLine +
                "DO NOT change the name of this issue. Names are used to identify the issues created by automation." + Environment.NewLine
        };

        public static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            using var di = BuildDependencyInjection(config);

            async Task Scanner(IEnumerable<string> repositories, Options options, IGitHubClient ghClient, GitHubConfiguration githubConfig)
            {
                var logger = di.GetService<ILogger<Program>>();
                var client = di.GetService<ValidationClient>();
                await client.Init();

                var start = DateTime.UtcNow;

                var results = new List<ValidationReport>();
                foreach (var repo in repositories)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    var result = await client.ValidateRepository(githubConfig.Organization, repo, options.IgnoreRepositoryRules).ConfigureAwait(false);
                    results.Add(result);
                }

                ReportToConsole(logger, results);

                if (options.AutoFix)
                {
                    await PerformAutofixes(ghClient, results);
                }
                if (!string.IsNullOrWhiteSpace(options.CsvFile))
                {
                    ReportToCsv(di.GetService<ILogger<CsvReporter>>(), options.CsvFile, results);
                }
                if (options.ReportToGithub)
                {
                    await ReportToGitHub(ghClient, _gitHubReporterConfig, di.GetService<ILogger<GitHubReporter>>(), results);
                }
                if (options.ReportToSlack)
                {
                    var slackSection = config.GetSection("Slack");
                    if (slackSection.Exists())
                    {
                        var slackConfig = new SlackConfiguration();
                        slackSection.Bind(slackConfig);
                        await ReportToSlack(slackConfig, logger, results);
                    }
                }
                logger.LogInformation("Duration {duration}", (DateTime.UtcNow - start).TotalSeconds);
            }

            await Parser.Default
                .ParseArguments<ScanSelectedOptions, ScanAllOptions, GenerateDocumentationOptions>(args)
                .MapResult(
                    async (ScanSelectedOptions options) => await Scanner(options.Repositories, options, di.GetService<IGitHubClient>(), di.GetService<GitHubConfiguration>()),
                    async (ScanAllOptions options) =>
                    {
                        var githubConfig = di.GetService<GitHubConfiguration>();
                        var ghClient = di.GetService<IGitHubClient>();
                        var allNonArchivedRepositories = ghClient
                            .Repository
                            .GetAllForOrg(githubConfig.Organization)
                            .Result
                            .Where(repository => !repository.Archived);
                        await Scanner(allNonArchivedRepositories.Select(r => r.Name).ToArray(), options, ghClient, githubConfig);
                    },
                    async (GenerateDocumentationOptions options) =>
                    {
                        var documentCreator = di.GetService<DocumentationFileCreator>();
                        documentCreator.GenerateDocumentation(options.OutputFolder);

                        await Task.CompletedTask;
                    },
                    async errors => await Task.CompletedTask);
        }

        private static async Task PerformAutofixes(IGitHubClient ghClient, IEnumerable<ValidationReport> results)
        {
            foreach (var repositoryResult in results)
            {
                foreach (var ruleResult in repositoryResult.Results.Where(r => !r.IsValid))
                {
                    await ruleResult.Fix(ghClient, repositoryResult.Repository);
                }
            }
        }

        private static void ReportToConsole(ILogger logger, IEnumerable<ValidationReport> reports)
        {
            foreach (var report in reports)
            {
                logger.LogInformation($"{report.Owner}/{report.RepositoryName}");
                foreach (var error in report.Results)
                {
                    logger.LogInformation("Rule: '{ruleName}' Is valid: {isValid}{fix}", error.RuleName, error.IsValid, error.IsValid ? string.Empty : $", {error.HowToFix}");
                }
            }
        }

        private static async Task ReportToGitHub(IGitHubClient client, GitHubReportConfig config, ILogger<GitHubReporter> logger, IEnumerable<ValidationReport> reports)
        {
            var reporter = new GitHubReporter(logger, client, config);
            await reporter.Report(reports);
        }

        private static void ReportToCsv(ILogger<CsvReporter> logger, string fileName, IEnumerable<ValidationReport> reports)
        {
            var file = new FileInfo(fileName);
            var reporter = new CsvReporter(logger, file);
            reporter.Report(reports);
        }

        private static async Task ReportToSlack(SlackConfiguration config, ILogger logger, IEnumerable<ValidationReport> reports)
        {
            var slackClient = new SlackClient(config);
            using var response = await slackClient.SendMessageAsync(reports);
            var isValid = response.IsSuccessStatusCode ? "valid" : "invalid";
            logger.LogInformation("Received {isValid} response.", isValid);
        }

        private static GitHubClient CreateClient(GitHubConfiguration configuration)
        {
            var client = new GitHubClient(new ProductHeaderValue("PTCS-Repository-Validator"));
            if (!string.IsNullOrWhiteSpace(configuration.Token))
            {
                var tokenAuth = new Credentials(configuration.Token);
                client.Credentials = tokenAuth;
            }
            return client;
        }

        private static ServiceProvider BuildDependencyInjection(IConfiguration config)
        {
            return new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                })
                .AddValidationRules(config)
                .AddTransient(services =>
                {
                    var githubConfig = new GitHubConfiguration();
                    config.GetSection("GitHub").Bind(githubConfig);

                    ValidateConfig(githubConfig);
                    return githubConfig;
                })
                .AddTransient<IGitHubClient, GitHubClient>(services =>
                {
                    return CreateClient(services.GetService<GitHubConfiguration>());
                })
                .AddTransient<ValidationClient>()
                .AddSingleton<IRepositoryValidator>(provider =>
                {
                    return new RepositoryValidator(
                        provider.GetService<ILogger<RepositoryValidator>>(),
                        provider.GetService<IGitHubClient>(),
                        provider.GetServices<IValidationRule>().ToArray());
                })
                .AddTransient<GitUtils>()
                .AddTransient<DocumentationFileCreator>()
                .BuildServiceProvider();
        }

        private static void ValidateConfig(GitHubConfiguration gitHubConfiguration)
        {
            if (string.IsNullOrWhiteSpace(gitHubConfiguration.Organization))
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Organization), "Organization was missing.");
            }

            if (string.IsNullOrWhiteSpace(gitHubConfiguration.Token))
            {
                throw new ArgumentNullException(nameof(gitHubConfiguration.Token), "Token was missing.");
            }
        }
    }
}
