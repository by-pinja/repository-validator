using CommandLine;

namespace StatusFunction.Console
{
    [Verb("read-status", HelpText = "Reads status from service. This can be used to verify connection.")]
    public class ReadTargetOptions
    {
        [Option('r', "ResourceGroup", Required = true, HelpText = "Name of the resource group which has the alerts.")]
        public string ResourceGroup { get; set; }

        [Option('s', "SubscriptionId", Required = true, HelpText = "ID of the subscription which contains the resource group which has the alerts.")]
        public string SubscriptionId { get; set; }
    }
}
