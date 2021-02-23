using CommandLine;

namespace StatusFunction.Console
{
    [Verb("save-status", HelpText = "Reads status from service and saves it to table storage.")]
    public class SaveTargetOptions
    {
        [Option('r', "ResourceGroup", Required = true, HelpText = "Name of the resource group which has the alerts.")]
        public string ResourceGroup { get; set; }

        [Option('s', "SubscriptionId", Required = true, HelpText = "ID of the subscription which contains the resource group which has the alerts.")]
        public string SubscriptionId { get; set; }
    }
}
