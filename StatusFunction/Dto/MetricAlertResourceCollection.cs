using System.Collections.Generic;

namespace StatusFunction.Dto
{
    /// <summary>
    /// Represents https://docs.microsoft.com/en-us/rest/api/monitor/metricalerts/listbyresourcegroup#metricalertresourcecollection
    /// </summary>   
    public class MetricAlertResourceCollection
    {
        public List<MetricAlertResource> Value { get; set; }
    }
}
