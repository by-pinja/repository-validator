using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StatusFunction.Console
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            using var di = BuildDependencyInjection(config);

            await Parser.Default
                .ParseArguments<ReadTargetOptions, SaveTargetOptions, ReadCacheOptions>(args)
                .MapResult(
                    async (ReadTargetOptions options) =>
                    {
                        var apiWrapper = di.GetService<AlertApiWrapper>();
                        var logger = di.GetService<ILogger<Program>>();
                        var result = await apiWrapper.GetStatus(options.SubscriptionId, options.ResourceGroup);
                        logger.LogTrace("Alert rules defined: {isNotNull}", result.AlertRules != null);

                        logger.LogInformation("Rules found: {ruleCount}", result.AlertRules.Value.Count);
                        foreach (var rule in result.AlertRules.Value)
                        {
                            logger.LogInformation("ID: {id}", rule.Id);
                            logger.LogInformation("Name: {name}", rule.Name);
                        }

                        logger.LogInformation("Alerts: {alertCount}", result.Alerts.Value.Count);
                        foreach (var alert in result.Alerts.Value)
                        {
                            logger.LogInformation("Alert: {name}", alert.Name);
                            logger.LogInformation("Rule: {rule}", alert.Properties.Essentials.AlertRule);
                            logger.LogInformation("Condition: {condition}", alert.Properties.Essentials.MonitorCondition);
                            logger.LogInformation("Condition resolved date time: {time}", alert.Properties.Essentials.MonitorConditionResolvedDateTime);
                        }
                    },
                    async (SaveTargetOptions options) =>
                    {
                        var apiWrapper = di.GetService<AlertApiWrapper>();
                        var storage = di.GetService<StatusCache>();
                        var logger = di.GetService<ILogger<Program>>();
                        var status = await apiWrapper.GetStatus(options.SubscriptionId, options.ResourceGroup);
                        logger.LogTrace("Alert rules defined: {isNotNull}", status.AlertRules != null);

                        var statusEntity = new StatusEntity("1")
                        {
                            Generated = DateTime.UtcNow,
                            Version = "1",
                            AlertRules = status.AlertRules.Value,
                            Alerts = status.Alerts.Value
                        };
                        await storage.InsertOrMerge(statusEntity);

                        logger.LogInformation("Rules found: {ruleCount}", status.AlertRules.Value.Count);
                    },
                    async (ReadCacheOptions options) =>
                    {
                        var storage = di.GetService<StatusCache>();
                        var logger = di.GetService<ILogger<Program>>();
                        logger.LogInformation("Reading monitoring status from cache...");

                        var status = (await storage.GetStatus()).FirstOrDefault();
                        if (status == null)
                        {
                            logger.LogWarning("No cached status found!");
                            return;
                        }

                        logger.LogInformation("Cache last updated {updateTime}", status.Generated);
                        logger.LogInformation("Rules found: {ruleCount}", status.AlertRules.Count);
                        foreach (var rule in status.AlertRules)
                        {
                            logger.LogInformation("ID: {id}", rule.Id);
                            logger.LogInformation("Name: {name}", rule.Name);
                        }

                        logger.LogInformation("Alerts: {alertCount}", status.Alerts.Count);
                        foreach (var alert in status.Alerts)
                        {
                            logger.LogInformation("Alert: {name}", alert.Name);
                            logger.LogInformation("Rule: {rule}", alert.Properties.Essentials.AlertRule);
                            logger.LogInformation("Condition: {condition}", alert.Properties.Essentials.MonitorCondition);
                            logger.LogInformation("Condition resolved date time: {time}", alert.Properties.Essentials.MonitorConditionResolvedDateTime);
                        }
                    },
                    async errors => await Task.CompletedTask);
        }

        private static ServiceProvider BuildDependencyInjection(IConfiguration config)
        {
            var tableStorageSettings = config.GetSection("TableStorage").Get<TableStorageSettings>();

            return new ServiceCollection()
                .AddSingleton(tableStorageSettings)
                .AddTransient<AlertApiWrapper>()
                .AddTransient<StatusCache>()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConfiguration(config.GetSection("Logging"));
                    loggingBuilder.AddConsole();
                })
                .BuildServiceProvider();
        }
    }
}
