using System.Collections.Generic;

namespace StatusFunction.Dto
{
    /// <summary>
    /// Represents alertList
    /// https://docs.microsoft.com/en-us/rest/api/monitor/alertsmanagement/alerts/getall#alertslist
    /// </summary>
    public class AlertList
    {
        public string NextLink { get; set; }
        public List<Alert> Value { get; set; }
    }
}
