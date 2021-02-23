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
        private readonly AlertApiWrapper _apiWrapper;
        private readonly StatusCache _statusCache;

        public StatusEndpoint(ILogger<StatusEndpoint> logger, StatusMonitoringConfiguration config, AlertApiWrapper apiWrapper, StatusCache statusCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _apiWrapper = apiWrapper ?? throw new ArgumentNullException(nameof(apiWrapper));
            _statusCache = statusCache;
        }

        [FunctionName("GetAlertsAndRules")]
        public async Task<IActionResult> GetAlertsAndRules([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/monitor")] HttpRequestMessage req)
        {
            if (req is null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            _logger.LogDebug("Fetching metric alert rules for {resourceGroup}", _config.ResourceGroup);
            var status = await _apiWrapper.GetStatus(_config.SubscriptionId, _config.ResourceGroup);

            var statusEntity = new StatusEntity("1")
            {
                Generated = DateTime.UtcNow,
                Version = "1",
                AlertRules = status.Rules,
                Alerts = status.Alerts
            };
            await _statusCache.InsertOrMerge(statusEntity);

            return new JsonResult(status);
        }
    }
}
