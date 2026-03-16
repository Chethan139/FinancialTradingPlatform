using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.CosmosDb;

/// <summary>
/// Cosmos DB context for high-throughput, low-latency data
/// Used for: Market data, order book, real-time positions, event sourcing
/// 
/// Why Cosmos DB for Trading:
/// - Multi-region writes for global availability
/// - Automatic indexing for fast queries
/// - Change feed for real-time processing
/// - Guaranteed single-digit millisecond latency
/// - Elastic scalability (1M+ ops/sec)
/// 
/// Partitioning Strategy:
/// - Orders: Partitioned by UserId (isolates user data)
/// - Market Data: Partitioned by Symbol (hot partition for popular stocks)
/// - Events: Partitioned by AggregateId (event sourcing pattern)
/// 
/// Interview Key Points:
/// - RU (Request Units) provisioning for cost optimization
/// - Partition key selection for even distribution
/// - Consistency levels: Session (default), Eventual, Strong
/// - TTL for automatic data expiration
/// </summary>
public class CosmosDbContext
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<CosmosDbContext> _logger;
    private readonly string _databaseName;

    // Container names
    public const string OrdersContainerName = "Orders";
    public const string PortfoliosContainerName = "Portfolios";
    public const string MarketDataContainerName = "MarketData";
    public const string EventStoreContainerName = "EventStore";

    public CosmosDbContext(IConfiguration configuration, ILogger<CosmosDbContext> logger)
    {
        _logger = logger;
        _databaseName = configuration["CosmosDb:DatabaseName"] ?? "TradingPlatform";

        var connectionString = configuration["CosmosDb:ConnectionString"];
        var endpoint = configuration["CosmosDb:Endpoint"];
        var key = configuration["CosmosDb:Key"];

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Use connection string (for development)
            _cosmosClient = new CosmosClient(connectionString);
        }
        else if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
        {
            // Use endpoint + key (for production)
            var options = new CosmosClientOptions
            {
                ApplicationName = "FinancialTradingPlatform",
                ConnectionMode = ConnectionMode.Direct, // Better performance than Gateway
                MaxRetryAttemptsOnRateLimitedRequests = 9,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                ConsistencyLevel = ConsistencyLevel.Session, // Balance of consistency and performance
                AllowBulkExecution = true // Enable bulk operations for batch processing
            };

            _cosmosClient = new CosmosClient(endpoint, key, options);
        }
        else
        {
            throw new InvalidOperationException(
                "CosmosDb configuration missing. Provide either ConnectionString or Endpoint+Key");
        }

        _logger.LogInformation("Cosmos DB client initialized for database: {DatabaseName}", _databaseName);
    }

    /// <summary>
    /// Initialize database and containers with proper partitioning
    /// Called during application startup
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Create database with autoscale throughput (cost-effective for variable workloads)
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _databaseName,
                ThroughputProperties.CreateAutoscaleThroughput(4000) // 400-4000 RU/s autoscale
            );

            var database = databaseResponse.Database;
            _logger.LogInformation("Database {DatabaseName} ready", _databaseName);

            // Create Orders container (partitioned by UserId)
            await database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = OrdersContainerName,
                PartitionKeyPath = "/userId",
                DefaultTimeToLive = -1, // No TTL (keep all orders)
                IndexingPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths =
                    {
                        new IncludedPath { Path = "/*" }
                    },
                    ExcludedPaths =
                    {
                        new ExcludedPath { Path = "/\"_etag\"/?" } // Exclude etag from indexing
                    }
                }
            });

            // Create Portfolios container (partitioned by UserId)
            await database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = PortfoliosContainerName,
                PartitionKeyPath = "/userId",
                DefaultTimeToLive = -1
            });

            // Create MarketData container (partitioned by Symbol)
            // TTL of 7 days - historical data archived to cheaper storage
            await database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = MarketDataContainerName,
                PartitionKeyPath = "/symbol",
                DefaultTimeToLive = 604800, // 7 days in seconds
                IndexingPolicy = new IndexingPolicy
                {
                    // Optimize for write-heavy workloads
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true
                }
            });

            // Create EventStore container for event sourcing (partitioned by AggregateId)
            await database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = EventStoreContainerName,
                PartitionKeyPath = "/aggregateId",
                DefaultTimeToLive = -1, // Keep all events forever (audit trail)
                // Enable change feed for real-time processing
                ChangeFeedPolicy = new ChangeFeedPolicy
                {
                    FullFidelityRetention = TimeSpan.FromMinutes(5)
                }
            });

            _logger.LogInformation("All Cosmos DB containers initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Cosmos DB");
            throw;
        }
    }

    /// <summary>
    /// Get container reference
    /// </summary>
    public Container GetContainer(string containerName)
    {
        return _cosmosClient.GetContainer(_databaseName, containerName);
    }

    /// <summary>
    /// Get change feed processor for real-time event processing
    /// Used to react to data changes in near real-time
    /// </summary>
    public ChangeFeedProcessor GetChangeFeedProcessor(
        string containerName,
        string processorName,
        Container leaseContainer,
        ChangesHandler<dynamic> onChangesDelegate)
    {
        var container = GetContainer(containerName);

        return container
            .GetChangeFeedProcessorBuilder(processorName, onChangesDelegate)
            .WithInstanceName($"{processorName}-{Environment.MachineName}")
            .WithLeaseContainer(leaseContainer)
            .WithStartTime(DateTime.UtcNow.AddHours(-1)) // Start from 1 hour ago
            .WithPollInterval(TimeSpan.FromSeconds(1)) // Check for changes every second
            .Build();
    }

    /// <summary>
    /// Dispose Cosmos client
    /// </summary>
    public void Dispose()
    {
        _cosmosClient?.Dispose();
    }
}
