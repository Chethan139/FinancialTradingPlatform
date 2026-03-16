using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Azure.CosmosDb;

/// <summary>
/// Cosmos DB service for high-throughput, low-latency data
/// Used for: Market data, real-time prices, order books, trade history
/// 
/// Why Cosmos DB:
/// - Global distribution with multi-region writes
/// - Guaranteed <10ms latency at P99
/// - Automatic indexing and multi-model support
/// - Elastic scale for high-throughput scenarios
/// - Multiple consistency models (Strong, Bounded Staleness, Session, Eventual)
/// </summary>
public class CosmosDbService<T> where T : class
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbService<T>> _logger;

    public CosmosDbService(
        CosmosClient cosmosClient,
        string databaseName,
        string containerName,
        ILogger<CosmosDbService<T>> logger)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    /// <summary>
    /// Get item by ID and partition key
    /// </summary>
    public async Task<T> GetItemAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            _logger.LogInformation("Retrieved item {Id} consuming {RU} RUs", id, response.RequestCharge);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Item {Id} not found in partition {PartitionKey}", id, partitionKey);
            throw;
        }
    }

    /// <summary>
    /// Query items using SQL-like syntax
    /// </summary>
    public async Task<IEnumerable<T>> GetItemsAsync(string queryString)
    {
        var query = _container.GetItemQueryIterator<T>(new QueryDefinition(queryString));
        var results = new List<T>();
        double totalRU = 0;

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
            totalRU += response.RequestCharge;
        }

        _logger.LogInformation("Query completed consuming {RU} RUs", totalRU);
        return results;
    }

    /// <summary>
    /// Create new item
    /// </summary>
    public async Task<T> CreateItemAsync(T item, string partitionKey)
    {
        var response = await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
        _logger.LogInformation("Created item consuming {RU} RUs", response.RequestCharge);
        return response.Resource;
    }

    /// <summary>
    /// Upsert (create or replace) item
    /// </summary>
    public async Task<T> UpsertItemAsync(T item, string partitionKey)
    {
        var response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey));
        return response.Resource;
    }

    /// <summary>
    /// Delete item
    /// </summary>
    public async Task DeleteItemAsync(string id, string partitionKey)
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
        _logger.LogInformation("Deleted item {Id}", id);
    }

    /// <summary>
    /// Batch operations for efficiency
    /// </summary>
    public async Task<IEnumerable<T>> BatchCreateAsync(IEnumerable<T> items, string partitionKey)
    {
        var batch = _container.CreateTransactionalBatch(new PartitionKey(partitionKey));
        
        foreach (var item in items)
        {
            batch.CreateItem(item);
        }

        var response = await batch.ExecuteAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Batch operation failed with status {StatusCode}", response.StatusCode);
            throw new Exception($"Batch operation failed: {response.ErrorMessage}");
        }

        return response.GetOperationResults<T>().Select(r => r.Resource);
    }
}
