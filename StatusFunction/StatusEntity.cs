using System;
using Microsoft.Azure.Cosmos.Table;

namespace StatusFunction
{
    /// <summary>
    /// DTO for the alert that is stored in table storage
    /// </summary>
    public class StatusEntity : TableEntity
    {
        public DateTime Generated {get;set;}
        public string Version {get;set;}
        public dynamic[] Alerts {get;set;}
        public dynamic[] AlertRules {get;set;}

        public StatusEntity()
        {

        }

        public StatusEntity(string version)
        {
            RowKey = version;
            PartitionKey = version;
        }
    }
}
