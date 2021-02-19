using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent;

using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using RestSharp;

namespace ValidationLibrary.AzureFunctions
{
    public class StatusEndpoint
    {
        private readonly ILogger<StatusEndpoint> _logger;
        private readonly IRepositoryValidator _validator;
        private readonly IAzure _azure;

        public StatusEndpoint(ILogger<StatusEndpoint> logger, IRepositoryValidator validator, IAzure azure)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _azure = azure ?? throw new ArgumentNullException(nameof(azure));
        }

        [FunctionName("StatusCheck")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/status")] HttpRequestMessage req)
        {
            if (req is null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            _logger.LogDebug("Repository validator status check hook launched. URI: {uri}", req.RequestUri);

            return new JsonResult(new
            {
                Rules = _validator.Rules.Select(r => r.GetConfiguration()).ToArray()
            });
        }

        [FunctionName("Monitor")]
        public async Task<IActionResult> MonitorOverview([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v2/status")] HttpRequestMessage req)
        {
            if (req is null)
            {
                throw new ArgumentNullException(nameof(req));
            }


            var rules = await _azure.AlertRules.MetricAlerts.ListByResourceGroupAsync("hjni-repo-dev");

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com");

            var client = new RestClient("https://management.azure.com");
            client.AddDefaultHeader("Authorization", $"Bearer {accessToken}");

            var request = new RestRequest("subscriptions/af9079ac-7689-45f2-b34f-7a7ba9a45665/providers/Microsoft.AlertsManagement/alerts", DataFormat.Json);
            request.AddQueryParameter("api-version", "2019-03-01");

            var response = client.Get(request);

            return new JsonResult(new
            {
                Rules = rules.Select(r => r.Name + r.Severity).ToArray(),
                Response = response.Content
            });
        }
    }
}
