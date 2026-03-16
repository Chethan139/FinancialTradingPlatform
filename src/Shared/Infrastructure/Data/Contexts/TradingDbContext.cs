using Common.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Contexts;

/// <summary>
/// SQL Server database context for transactional data
/// Handles Orders, Portfolios, Positions, Transactions
/// Implements optimistic concurrency, soft delete, and audit logging
/// 
/// Why SQL Server for this data:
/// - ACID transactions critical for financial data
/// - Complex joins and reporting queries
/// - Strong consistency requirements
/// - Relational integrity constraints
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
            entity.Property(e => e.RowVersion).IsRowVersion(); // Optimistic concurrency
            entity.HasQueryFilter(e => !e.IsDeleted); // Global query filter for soft delete
            
            // Precision for financial data
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.LimitPrice).HasPrecision(18, 8);
            entity.Property(e => e.StopPrice).HasPrecision(18, 8);
            entity.Property(e => e.FilledQuantity).HasPrecision(18, 8);
            entity.Property(e => e.AverageFillPrice).HasPrecision(18, 8);
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
            entity.Property(e => e.Commission).HasPrecision(18, 2);
            entity.Property(e => e.RiskScore).HasPrecision(5, 2);
        });

        // Portfolio configuration
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            // Relationships
            entity.HasMany(p => p.Positions)
                  .WithOne(pos => pos.Portfolio)
                  .HasForeignKey(pos => pos.PortfolioId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasMany(p => p.Transactions)
                  .WithOne(t => t.Portfolio)
                  .HasForeignKey(t => t.PortfolioId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Financial precision
            entity.Property(e => e.CashBalance).HasPrecision(18, 2);
            entity.Property(e => e.TotalValue).HasPrecision(18, 2);
            entity.Property(e => e.InvestedCapital).HasPrecision(18, 2);
            entity.Property(e => e.TotalProfitLoss).HasPrecision(18, 2);
            entity.Property(e => e.DailyProfitLoss).HasPrecision(18, 2);
            entity.Property(e => e.RiskScore).HasPrecision(5, 2);
        });

        // Position configuration
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PortfolioId);
            entity.HasIndex(e => e.Symbol);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasQueryFilter(e => !e.IsDeleted);
            
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.AverageCost).HasPrecision(18, 8);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 8);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.MarketValue).HasPrecision(18, 2);
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
            
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Commission).HasPrecision(18, 2);
            entity.Property(e => e.NetAmount).HasPrecision(18, 2);
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically set audit fields
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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
                // Prevent modification of CreatedAt and CreatedBy
                entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
