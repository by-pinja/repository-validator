using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using StatusFunction.Dto;

namespace StatusFunction
{
    /// <summary>
    /// DTO for the alert that is stored in table storage
    /// </summary>
    public class StatusEntity : TableEntity
    {
        public DateTime Generated { get; set; }
        public string Version { get; set; }

        [IgnoreProperty]
        public List<Alert> Alerts { get; set; }

        [IgnoreProperty]
        public List<MetricAlertResource> AlertRules { get; set; }

        public StatusEntity()
        {

        }

        public StatusEntity(string version)
        {
            RowKey = version;
            PartitionKey = version;
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            // Lists are manually serialized because lists are not supported
            // https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#tables-entities-and-properties
            var baseEntity = base.WriteEntity(operationContext);
            baseEntity[nameof(Alerts)] = new EntityProperty(JsonConvert.SerializeObject(Alerts));
            baseEntity[nameof(AlertRules)] = new EntityProperty(JsonConvert.SerializeObject(AlertRules));
            return baseEntity;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            // Lists are manually deserialized because lists are not supported
            // https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#tables-entities-and-properties
            base.ReadEntity(properties, operationContext);
            if (properties.ContainsKey(nameof(Alerts)))
            {
                Alerts = JsonConvert.DeserializeObject<List<Alert>>(properties[nameof(Alerts)].StringValue);
            }
            if (properties.ContainsKey(nameof(AlertRules)))
            {
                AlertRules = JsonConvert.DeserializeObject<List<MetricAlertResource>>(properties[nameof(AlertRules)].StringValue);
            }
        }
    }
}
