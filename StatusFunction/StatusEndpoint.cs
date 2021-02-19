using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using RestSharp;

namespace ValidationLibrary.AzureFunctions
{
    public class StatusEndpoint
    {
        private readonly string _resourceGroup = "hjni-repo-dev";
        private readonly string _subscriptionId = "af9079ac-7689-45f2-b34f-7a7ba9a45665";

        private readonly ILogger<StatusEndpoint> _logger;

        public StatusEndpoint(ILogger<StatusEndpoint> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [FunctionName("GetAlertsAndRules")]
        public async Task<IActionResult> GetAlertsAndRules([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/monitor")] HttpRequestMessage req)
        {
            if (req is null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            _logger.LogDebug("Fetching metric alert rules for {resourceGroup}", _resourceGroup);
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com");

            var client = new RestClient("https://management.azure.com");
            client.AddDefaultHeader("Authorization", $"Bearer {accessToken}");

            var ruleRequest = new RestRequest($"subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroup}/providers/Microsoft.Insights/metricAlerts", DataFormat.Json);
            ruleRequest.AddQueryParameter("api-version", "2018-03-01");
            var ruleResponse = client.Get(ruleRequest);

            _logger.LogDebug("Fetching alerts for {resourceGroup}", _resourceGroup);
            //GET https://management.azure.com/{scope}/providers/Microsoft.AlertsManagement/alerts?targetResource={targetResource}&targetResourceType={targetResourceType}&targetResourceGroup={targetResourceGroup}&monitorService={monitorService}&monitorCondition={monitorCondition}&severity={severity}&alertState={alertState}&alertRule={alertRule}&smartGroupId={smartGroupId}&includeContext={includeContext}&includeEgressConfig={includeEgressConfig}&pageCount={pageCount}&sortBy={sortBy}&sortOrder={sortOrder}&select={select}&timeRange={timeRange}&customTimeRange={customTimeRange}&api-version=2019-03-01
            var alertRequest = new RestRequest($"subscriptions/{_subscriptionId}/providers/Microsoft.AlertsManagement/alerts", DataFormat.Json);
            alertRequest.AddQueryParameter("api-version", "2019-03-01");
            alertRequest.AddQueryParameter("targetResourceGroup", _resourceGroup);
            var alertResponse = client.Get(alertRequest);

            return new JsonResult(new
            {
                Rules = ruleResponse.Content,
                Response = alertResponse.Content
            });
        }
    }
}
