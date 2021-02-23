using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StatusFunction;

[assembly: WebJobsStartup(typeof(Startup))]
namespace StatusFunction
{
    internal class CustomTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _version;

        public CustomTelemetryInitializer()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            _version = fileVersionInfo.ProductVersion;
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Component.Version = _version;
        }
    }

    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var statusConfig = config.GetSection("StatusMonitoringConfiguration").Get<StatusMonitoringConfiguration>();
            var tableStorageSettings = config.GetSection("TableStorage").Get<TableStorageSettings>();

            builder
                .Services
                .AddLogging()
                .AddSingleton(statusConfig)
                .AddSingleton(tableStorageSettings)
                .AddTransient<StatusCache>()
                .AddTransient<AlertApiWrapper>()
                .AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
        }
    }
}
