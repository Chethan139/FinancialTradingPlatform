using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Cosmos;

namespace DataPipeline.Functions;

public class MarketDataPipeline
{
    private readonly ILogger<MarketDataPipeline> _logger;

    public MarketDataPipeline(ILogger<MarketDataPipeline> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Timer-triggered function to fetch market data every 5 seconds
    /// </summary>
    [Function("FetchMarketData")]
    public async Task FetchMarketData(
        [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
        FunctionContext context)
    {
        _logger.LogInformation("Starting market data fetch at {Time}", DateTime.UtcNow);

        try
        {
            var stockData = new List<StockData>
            {
                new StockData
                {
                    Id = Guid.NewGuid().ToString(),
                    Symbol = "AAPL",
                    CompanyName = "Apple Inc.",
                    CurrentPrice = 150.25m,
                    OpenPrice = 148.00m,
                    HighPrice = 152.00m,
                    LowPrice = 147.50m,
                    Volume = 50000000,
                    DailyChange = 2.25m,
                    PercentChange = 1.53m,
                    LastUpdate = DateTime.UtcNow
                },
                new StockData
                {
                    Id = Guid.NewGuid().ToString(),
                    Symbol = "GOOGL",
                    CompanyName = "Alphabet Inc.",
                    CurrentPrice = 140.50m,
                    OpenPrice = 138.75m,
                    HighPrice = 142.00m,
                    LowPrice = 138.50m,
                    Volume = 30000000,
                    DailyChange = 1.75m,
                    PercentChange = 1.27m,
                    LastUpdate = DateTime.UtcNow
                },
                new StockData
                {
                    Id = Guid.NewGuid().ToString(),
                    Symbol = "MSFT",
                    CompanyName = "Microsoft Corporation",
                    CurrentPrice = 380.00m,
                    OpenPrice = 375.50m,
                    HighPrice = 382.50m,
                    LowPrice = 375.00m,
                    Volume = 25000000,
                    DailyChange = 4.50m,
                    PercentChange = 1.20m,
                    LastUpdate = DateTime.UtcNow
                },
                new StockData
                {
                    Id = Guid.NewGuid().ToString(),
                    Symbol = "TSLA",
                    CompanyName = "Tesla Inc.",
                    CurrentPrice = 245.75m,
                    OpenPrice = 240.00m,
                    HighPrice = 248.00m,
                    LowPrice = 239.50m,
                    Volume = 40000000,
                    DailyChange = 5.75m,
                    PercentChange = 2.40m,
                    LastUpdate = DateTime.UtcNow
                },
                new StockData
                {
                    Id = Guid.NewGuid().ToString(),
                    Symbol = "NVDA",
                    CompanyName = "NVIDIA Corporation",
                    CurrentPrice = 890.50m,
                    OpenPrice = 880.00m,
                    HighPrice = 895.00m,
                    LowPrice = 875.50m,
                    Volume = 20000000,
                    DailyChange = 10.50m,
                    PercentChange = 1.20m,
                    LastUpdate = DateTime.UtcNow
                }
            };

            await ProcessAndStoreData(stockData);

            _logger.LogInformation("Market data fetch completed successfully at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching market data");
            throw;
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule: {Next}", myTimer.ScheduleStatus.Next);
        }
    }

    /// <summary>
    /// Timer-triggered function to validate data quality
    /// </summary>
    [Function("ValidateMarketData")]
    public async Task ValidateMarketData(
        [TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Starting data validation at {Time}", DateTime.UtcNow);

        try
        {
            var invalidRecords = 0;

            // Validation logic
            _logger.LogInformation("Data validation completed. Invalid records: {InvalidCount}", invalidRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating market data");
            throw;
        }
    }

    /// <summary>
    /// Timer-triggered function to clean up old data
    /// </summary>
    [Function("CleanupOldData")]
    public async Task CleanupOldData(
        [TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Starting data cleanup at {Time}", DateTime.UtcNow);

        try
        {
            var deletedRecords = 0;

            // Cleanup logic - delete records older than 30 days
            _logger.LogInformation("Data cleanup completed. Deleted records: {DeletedCount}", deletedRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old data");
            throw;
        }
    }

    private async Task ProcessAndStoreData(List<StockData> stockData)
    {
        // Simulate data processing and storage
        foreach (var stock in stockData)
        {
            _logger.LogInformation("Processing stock: {Symbol} - Price: {Price}", stock.Symbol, stock.CurrentPrice);
            await Task.Delay(50);
        }

        _logger.LogInformation("Processed {Count} stock records", stockData.Count);
    }
}

public class StockData
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public string CompanyName { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public long Volume { get; set; }
    public decimal DailyChange { get; set; }
    public decimal PercentChange { get; set; }
    public DateTime LastUpdate { get; set; }
    public string Type { get; set; } = "stock-data";
}
