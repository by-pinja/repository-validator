namespace StatusFunction.Dto
{
    /// <summary>
    /// Represents a single metric alert resource
    /// https://docs.microsoft.com/en-us/rest/api/monitor/metricalerts/listbyresourcegroup#metricalertresource
    /// </summary>
    public class MetricAlertResource
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
