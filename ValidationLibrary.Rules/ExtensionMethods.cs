using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ValidationLibrary.Rules
{
    public static class ExtensionMethods
    {
        public static IServiceCollection AddValidationRules(this IServiceCollection service, IConfiguration config)
        {
            // Get all rule classes.            
            var allValidationRules = typeof(ExtensionMethods).Assembly.GetExportedTypes().Where(t => t.GetInterface(nameof(IValidationRule)) != null && !t.IsAbstract);

            // Select those rules defined by the configuration and the environment variables which should be disabled.
            var selectedValidationRules = allValidationRules.Where(r => !string.Equals(config[$"Rules:{r.Name}"], "disable", StringComparison.InvariantCultureIgnoreCase));

            // Add each rule as available for the dependency injection.
            foreach (var rule in selectedValidationRules)
            {
                service.AddTransient(typeof(IValidationRule), rule);
            }

            return service;
        }
    }
}
