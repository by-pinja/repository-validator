using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using ValidationLibrary.Rules;

namespace ValidationLibrary
{
    public class RepositoryValidator
    {
        private const string ConfigFileName = "repository-validator.json";

        private readonly ILogger _logger;

        private readonly IValidationRule[] _rules;

        public RepositoryValidator(ILogger logger)
        {
            _rules = new IValidationRule[]
            {
                new HasDescriptionRule(logger),
                new HasReadmeRule(logger),
                new HasNewestPtcsJenkinsLibRule(logger)
            };

            logger.LogInformation("Initializing {0} with rules: {1}", nameof(RepositoryValidator), string.Join(", ", _rules.Select(rule => rule.RuleName)));;
            _logger = logger;
        }

        /// <summary>
        /// Performs necessary initiation for all rules
        /// </summary>
        /// <param name="client">Github client</param>
        public async Task Init(GitHubClient client)
        {
            _logger.LogInformation("Initializing repository validator");
            foreach (var rule in _rules)
            {
                await rule.Init(client);
            }
        }

        public async Task<ValidationReport> Validate(GitHubClient client, Repository gitHubRepository)
        {
            _logger.LogTrace("Validating repository {0}", gitHubRepository.FullName);
            var config = await GetConfig(client, gitHubRepository);

            var filteredRules = _rules.Where(rule => 
            {
                var name = rule.GetType().Name;
                var isIgnored = config.IgnoredRules.Contains(name);
                _logger.LogTrace("Rule {0} ignore status: {1}", name, isIgnored);
                return !isIgnored;
            });

            var validationResults = await Task.WhenAll(filteredRules.Select(async rule => await rule.IsValid(client, gitHubRepository)));
            return new ValidationReport
            {
                Owner = gitHubRepository.Owner.Login,
                RepositoryName = gitHubRepository.Name,
                RepositoryUrl = gitHubRepository.HtmlUrl,
                Results = validationResults.ToArray()
            };
        }

        private async Task<ValidationConfiguration> GetConfig(GitHubClient client, Repository gitHubRepository)
        {
            try {
                _logger.LogTrace("Retrieving config for {0}", gitHubRepository.FullName);
                var contents = await client.Repository.Content.GetAllContents(gitHubRepository.Owner.Login, gitHubRepository.Name, ConfigFileName);
                var jsonContent = contents.FirstOrDefault().Content;
                var config = JsonConvert.DeserializeObject<ValidationConfiguration>(jsonContent);
                _logger.LogDebug("Configuration found for {0}. Ignored rules: {1}", gitHubRepository.FullName, string.Join(",", config.IgnoredRules));
                return config;
            } catch (Octokit.NotFoundException) {
                _logger.LogDebug("No {0} found in {1}. Using default config.", ConfigFileName, gitHubRepository.FullName);
                return new ValidationConfiguration();
            }
        }
    }
}