using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace StatusFunction
{
    /// <summary>
    /// This class hides implementation details of API communication.
    /// 
    /// This is a very quick and dirty implementation which should be refactored to proper interface etc.
    /// when the viability of this approach has been verified. Also when refactoring, more research should
    /// be done on already existing libraries.
    /// </summary>
    public class AlertApiWrapper
    {
        private readonly ILogger<AlertApiWrapper> _logger;

        public AlertApiWrapper(ILogger<AlertApiWrapper> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public async Task<dynamic> GetStatus(string subscriptionId, string resourecGroup)
        {
            /*
            NOTE: These rest calls should probably be refactored away when there is a proper library available which supports retriewing alerts and alert rules.
            Currently (2021-02-22) monitoring fluent API seemed to be at priview stage and didn't contain the actual alerts, only rules
            */

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");

            var client = new RestClient("https://management.azure.com")
                .AddDefaultHeader("Authorization", $"Bearer {accessToken}");

            var ruleRequest =
                new RestRequest($"subscriptions/{subscriptionId}/resourceGroups/{resourecGroup}/providers/Microsoft.Insights/metricAlerts", DataFormat.Json)
                .AddQueryParameter("api-version", "2018-03-01");
            var ruleResponse = client.Get<MetricAlertResponse>(ruleRequest);

            _logger.LogDebug("Fetching alerts for {resourceGroup}", resourecGroup);
            var alertRequest =
                new RestRequest($"subscriptions/{subscriptionId}/providers/Microsoft.AlertsManagement/alerts", DataFormat.Json)
                .AddQueryParameter("api-version", "2019-03-01")
                .AddQueryParameter("targetResourceGroup", resourecGroup);
            var alertResponse = client.Get(alertRequest);
            return new
            {
                Rules = ruleResponse.Data,
                Alerts = alertResponse
            };
        }
    }

    public class MetricAlertResponse
    {
        public dynamic[] Value { get; set; }
    }

    public class AlertResponse
    {
        public dynamic[] Value { get; set; }
    }
}
