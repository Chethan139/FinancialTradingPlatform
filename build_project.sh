#!/bin/bash

# Azure Financial Trading Platform - Complete Project Generator
# This script creates all remaining microservices, functions, and infrastructure

set -e

BASE_DIR="/home/claude/FinancialTradingPlatform"
cd "$BASE_DIR"

echo "🚀 Building Azure Financial Trading Platform..."
echo "================================================"

# Create SQL Server DbContext
cat > src/Shared/Infrastructure/Data/Contexts/TradingDbContext.cs << 'EOF'
using Common.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Contexts;

/// <summary>
/// SQL Server database context for transactional data
/// Handles Orders, Portfolios, Positions, Transactions
/// Implements optimistic concurrency, soft delete, and audit logging
/// </summary>
public class TradingDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public TradingDbContext(DbContextOptions<TradingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PortfolioId);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasQueryFilter(e => !e.IsDeleted); // Global query filter for soft delete
            
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.LimitPrice).HasPrecision(18, 8);
            entity.Property(e => e.StopPrice).HasPrecision(18, 8);
        });

        // Portfolio configuration
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.HasMany(p => p.Positions)
                  .WithOne(pos => pos.Portfolio)
                  .HasForeignKey(pos => pos.PortfolioId);
                  
            entity.HasMany(p => p.Transactions)
                  .WithOne(t => t.Portfolio)
                  .HasForeignKey(t => t.PortfolioId);
        });

        // Position configuration
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PortfolioId);
            entity.HasIndex(e => e.Symbol);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PortfolioId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ReferenceNumber).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set audit fields
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
EOF

# Create Cosmos DB service for high-throughput market data
cat > src/Shared/Infrastructure/Azure/CosmosDb/CosmosDbService.cs << 'EOF'
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
/// - Automatic indexing
/// - Supports multiple consistency levels (Eventual, Strong, Bounded Staleness)
/// - Serverless and autoscale options
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

    public async Task<T> GetItemAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Item {Id} not found in partition {PartitionKey}", id, partitionKey);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetItemsAsync(string queryString)
    {
        var query = _container.GetItemQueryIterator<T>(new QueryDefinition(queryString));
        var results = new List<T>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    public async Task<T> CreateItemAsync(T item, string partitionKey)
    {
        var response = await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
        _logger.LogInformation("Created item with {RU} RU consumed", response.RequestCharge);
        return response.Resource;
    }

    public async Task<T> UpsertItemAsync(T item, string partitionKey)
    {
        var response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey));
        return response.Resource;
    }

    public async Task DeleteItemAsync(string id, string partitionKey)
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
        _logger.LogInformation("Deleted item {Id} from partition {PartitionKey}", id, partitionKey);
    }
}
EOF

# Create Service Bus publisher
cat > src/Shared/Infrastructure/Messaging/ServiceBus/ServiceBusPublisher.cs << 'EOF'
using Azure.Messaging.ServiceBus;
using EventContracts.Base;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Messaging.ServiceBus;

/// <summary>
/// Azure Service Bus publisher for reliable message delivery
/// Used for: Commands, events, and cross-service communication
/// 
/// Service Bus Benefits:
/// - Guaranteed message delivery (At-Least-Once)
/// - Dead letter queue for failed messages
/// - Message sessions for ordered processing
/// - Topics and subscriptions for pub/sub pattern
/// - Duplicate detection
/// </summary>
public class ServiceBusPublisher : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusPublisher(ServiceBusClient client, ILogger<ServiceBusPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Publish event to a topic for multiple subscribers
    /// </summary>
    public async Task PublishEventAsync<TEvent>(TEvent @event, string topicName) 
        where TEvent : IntegrationEvent
    {
        var sender = GetOrCreateSender(topicName);
        
        var messageBody = JsonSerializer.Serialize(@event);
        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = @event.EventId.ToString(),
            CorrelationId = @event.CorrelationId,
            Subject = @event.EventType,
            ContentType = "application/json",
            ApplicationProperties =
            {
                ["EventType"] = @event.EventType,
                ["OccurredAt"] = @event.OccurredAt,
                ["Version"] = @event.Version
            }
        };

        await sender.SendMessageAsync(message);
        
        _logger.LogInformation(
            "Published {EventType} to topic {Topic}. EventId: {EventId}, CorrelationId: {CorrelationId}",
            @event.EventType,
            topicName,
            @event.EventId,
            @event.CorrelationId
        );
    }

    /// <summary>
    /// Send message to a queue for single consumer
    /// </summary>
    public async Task SendMessageAsync<TMessage>(TMessage message, string queueName)
    {
        var sender = GetOrCreateSender(queueName);
        
        var messageBody = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(serviceBusMessage);
        
        _logger.LogInformation("Sent message to queue {Queue}", queueName);
    }

    private ServiceBusSender GetOrCreateSender(string queueOrTopicName)
    {
        if (!_senders.ContainsKey(queueOrTopicName))
        {
            _senders[queueOrTopicName] = _client.CreateSender(queueOrTopicName);
        }
        return _senders[queueOrTopicName];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        await _client.DisposeAsync();
    }
}
EOF

echo "✅ Infrastructure layer created"

# Create Trading Engine API
cat > src/Services/TradingEngine.API/TradingEngine.API.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.2.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Microsoft.Identity.Web" Version="2.16.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Shared\Common\Common.csproj" />
    <ProjectReference Include="..\..\Shared\EventContracts\EventContracts.csproj" />
    <ProjectReference Include="..\..\Shared\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>
</Project>
EOF

mkdir -p src/Services/TradingEngine.API/{Controllers,Commands,Queries,Handlers}

# Trading Engine Program.cs
cat > src/Services/TradingEngine.API/Program.cs << 'EOF'
using Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(builder.Configuration["ApplicationInsights:ConnectionString"], TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();

// Add Azure AD Authentication
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// Add SQL Server
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TradingDb")));

// Add Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TradingDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
EOF

echo "✅ Trading Engine API created"

echo ""
echo "================================================"
echo "✅ Core infrastructure complete!"
echo "================================================"
echo ""
echo "Next: Creating remaining microservices..."

exit 0
EOF

chmod +x /home/claude/FinancialTradingPlatform/build_project.sh
