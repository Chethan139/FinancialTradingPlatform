using Common.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data.SqlServer;

/// <summary>
/// SQL Server DbContext for relational/transactional data
/// Used for: User profiles, portfolio snapshots, reporting, analytics
/// 
/// Why SQL Server + Cosmos DB (Polyglot Persistence):
/// - SQL Server: ACID transactions, complex joins, reporting, BI
/// - Cosmos DB: High throughput, low latency, global distribution
/// 
/// This is a realistic production pattern - different data stores for different needs
/// 
/// Interview Key Points:
/// - Polyglot persistence: Using the right database for each use case
/// - CQRS: SQL Server for read models, Cosmos for write models
/// - Eventual consistency: Data synced via events
/// - Connection pooling: Configured for high concurrency
/// </summary>
public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    // Read models for reporting and analytics
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Order> Orders { get; set; }

    // User and authentication
    public DbSet<UserProfile> UserProfiles { get; set; }

    // Reporting tables
    public DbSet<PortfolioSnapshot> PortfolioSnapshots { get; set; }
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Portfolio entity
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("Portfolios");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.IsActive);

            // Soft delete filter - only return non-deleted records
            entity.HasQueryFilter(p => !p.IsDeleted);

            // Precision for decimal fields
            entity.Property(p => p.CashBalance).HasPrecision(18, 2);
            entity.Property(p => p.TotalValue).HasPrecision(18, 2);
            entity.Property(p => p.InvestedCapital).HasPrecision(18, 2);
            entity.Property(p => p.TotalProfitLoss).HasPrecision(18, 2);

            // Relationships
            entity.HasMany(p => p.Positions)
                  .WithOne(pos => pos.Portfolio)
                  .HasForeignKey(pos => pos.PortfolioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Transactions)
                  .WithOne(t => t.Portfolio)
                  .HasForeignKey(t => t.PortfolioId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Row version for optimistic concurrency
            entity.Property(p => p.RowVersion).IsRowVersion();
        });

        // Configure Position entity
        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("Positions");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.PortfolioId, p.Symbol });
            entity.HasQueryFilter(p => !p.IsDeleted);

            entity.Property(p => p.Quantity).HasPrecision(18, 8);
            entity.Property(p => p.AverageCost).HasPrecision(18, 2);
            entity.Property(p => p.CurrentPrice).HasPrecision(18, 2);
            entity.Property(p => p.MarketValue).HasPrecision(18, 2);
            entity.Property(p => p.UnrealizedProfitLoss).HasPrecision(18, 2);
        });

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.PortfolioId);
            entity.HasIndex(t => t.OrderId);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasQueryFilter(t => !t.IsDeleted);

            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Commission).HasPrecision(18, 2);
            entity.Property(t => t.NetAmount).HasPrecision(18, 2);
        });

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.PortfolioId);
            entity.HasIndex(o => o.Symbol);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => o.IdempotencyKey).IsUnique();
            entity.HasQueryFilter(o => !o.IsDeleted);

            entity.Property(o => o.Quantity).HasPrecision(18, 8);
            entity.Property(o => o.LimitPrice).HasPrecision(18, 2);
            entity.Property(o => o.StopPrice).HasPrecision(18, 2);
            entity.Property(o => o.AverageFillPrice).HasPrecision(18, 2);
            entity.Property(o => o.TotalValue).HasPrecision(18, 2);
        });

        // Configure UserProfile entity
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.AzureAdUserId).IsUnique();
        });

        // Configure PortfolioSnapshot for reporting
        modelBuilder.Entity<PortfolioSnapshot>(entity =>
        {
            entity.ToTable("PortfolioSnapshots");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.PortfolioId, s.SnapshotDate });

            entity.Property(s => s.TotalValue).HasPrecision(18, 2);
            entity.Property(s => s.CashBalance).HasPrecision(18, 2);
            entity.Property(s => s.DailyReturn).HasPrecision(18, 4);
        });

        // Configure PerformanceMetric
        modelBuilder.Entity<PerformanceMetric>(entity =>
        {
            entity.ToTable("PerformanceMetrics");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.PortfolioId, m.MetricDate });
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically set audit fields
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically set audit fields
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically update audit fields on save
    /// </summary>
    private void UpdateAuditFields()
    {
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
    }
}

/// <summary>
/// User profile entity
/// </summary>
public class UserProfile : BaseEntity
{
    public string AzureAdUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string SubscriptionTier { get; set; } = "Free"; // Free, Premium, Enterprise
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Portfolio snapshot for historical tracking
/// Denormalized for efficient reporting
/// </summary>
public class PortfolioSnapshot : BaseEntity
{
    public Guid PortfolioId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public decimal TotalValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal InvestedCapital { get; set; }
    public decimal DailyReturn { get; set; }
    public decimal CumulativeReturn { get; set; }
    public int NumberOfPositions { get; set; }
}

/// <summary>
/// Performance metrics for analytics
/// </summary>
public class PerformanceMetric : BaseEntity
{
    public Guid PortfolioId { get; set; }
    public DateTime MetricDate { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal Volatility { get; set; }
    public decimal Beta { get; set; }
    public decimal Alpha { get; set; }
}
