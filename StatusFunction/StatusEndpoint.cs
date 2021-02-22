using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using RestSharp;

namespace StatusFunction
{
    public class StatusEndpoint
    {
        private readonly ILogger<StatusEndpoint> _logger;
        private readonly StatusMonitoringConfiguration _config;

        public StatusEndpoint(ILogger<StatusEndpoint> logger, StatusMonitoringConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        [FunctionName("GetAlertsAndRules")]
        public async Task<IActionResult> GetAlertsAndRules([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/monitor")] HttpRequestMessage req)
        {
            if (req is null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            _logger.LogDebug("Fetching metric alert rules for {resourceGroup}", _config.ResourceGroup);
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");

            /*
            NOTE: These rest calls should probably be refactored away when there is a proper library available which supports retriewing alerts and alert rules.
            Currently (2021-02-22) monitoring fluent API seemed to be at priview stage and didn't contain the actual alerts, only rules
            */

            var client = new RestClient("https://management.azure.com");
            client.AddDefaultHeader("Authorization", $"Bearer {accessToken}");

            var ruleRequest = new RestRequest($"subscriptions/{_config.SubscriptionId}/resourceGroups/{_config.ResourceGroup}/providers/Microsoft.Insights/metricAlerts", DataFormat.Json);
            ruleRequest.AddQueryParameter("api-version", "2018-03-01");
            var ruleResponse = client.Get(ruleRequest);

            _logger.LogDebug("Fetching alerts for {resourceGroup}", _config.ResourceGroup);
            var alertRequest = new RestRequest($"subscriptions/{_config.SubscriptionId}/providers/Microsoft.AlertsManagement/alerts", DataFormat.Json);
            alertRequest.AddQueryParameter("api-version", "2019-03-01");
            alertRequest.AddQueryParameter("targetResourceGroup", _config.ResourceGroup);
            var alertResponse = client.Get(alertRequest);

            return new JsonResult(new
            {
                Rules = ruleResponse.Content,
                Response = alertResponse.Content
            });
        }
    }
}
