namespace StatusFunction
{
    public class StatusMonitoringConfiguration
    {
        /// <summary>
        /// Name of the Resource group in Azure contains the alerts that are to
        /// be monitored.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// GUID for the subscription that contains the resource group which
        /// contains the alerts that are to be monitored.
        /// </summary>
        public string SubscriptionId { get; set; }
    }
}
