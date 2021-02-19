using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ValidationLibrary.AzureFunctions
{
    public class ConfigurationEndpoint
    {
        private readonly ILogger<ConfigurationEndpoint> _logger;
        private readonly IRepositoryValidator _validator;

        public ConfigurationEndpoint(ILogger<ConfigurationEndpoint> logger, IRepositoryValidator validator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        [FunctionName("ListConfigurations")]
        public IActionResult GetConfigurations([HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/configurations")] HttpRequestMessage req)
        {
            if (req is null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            _logger.LogDebug("Repository validator configuration list hook launched. URI: {uri}", req.RequestUri);

            return new JsonResult(new
            {
                Rules = _validator.Rules.Select(r => r.GetConfiguration()).ToArray()
            });
        }
    }
}
