namespace StatusFunction.Dto
{
    /// <summary>
    /// Alert properties
    /// 
    /// Represents
    /// https://docs.microsoft.com/en-us/rest/api/monitor/alertsmanagement/alerts/getall#alertproperties
    /// </summary>
    public class AlertProperties
    {
        public AlertEssential Essentials { get; set; }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/monitor/alertsmanagement/alerts/getall#essentials
    /// </summary>
    public class AlertEssential
    {
        public string AlertRule { get; set; }
        public string AlertState { get; set; }
        public string LastModifiedDateTime { get; set; }
        public string MonitorCondition { get; set; }
        public string MonitorConditionResolvedDateTime { get; set; }
        public string StartDateTime { get; set; }
    }
}
