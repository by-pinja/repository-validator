using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace StatusFunction
{
    /// <summary>
    /// Quick and dirty cache implementation
    /// </summary>
    public class StatusCache
    {
        private readonly ILogger<StatusCache> _logger;

        private readonly TableStorageSettings _settings;

        private readonly CloudTable _cacheTable;

        public StatusCache(ILogger<StatusCache> logger, TableStorageSettings settings)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            var storageAccount = CloudStorageAccount.Parse(_settings.ConnectionString);
            var client = storageAccount.CreateCloudTableClient();
            _cacheTable = client.GetTableReference(_settings.CacheTable);
            if (_cacheTable.CreateIfNotExists())
            {
                _logger.LogTrace("Table {table} doesn't exist, created.", _settings.CacheTable);
            }
        }

        public async Task InsertOrMerge(StatusEntity entity)
        {
            var insertOperation = TableOperation.InsertOrMerge(entity);
            await _cacheTable.ExecuteAsync(insertOperation);
        }
    }

    public class TableStorageSettings
    {
        public string CacheTable { get; set; }
        public string ConnectionString { get; set; }
    }
}
