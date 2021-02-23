namespace StatusFunction.Dto
{
    /// <summary>
    /// An alert created in alert management service.
    /// 
    /// Represents
    /// https://docs.microsoft.com/en-us/rest/api/monitor/alertsmanagement/alerts/getall#alert
    /// </summary>
    public class Alert
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public AlertProperties Properties { get; set; }
    }
}
