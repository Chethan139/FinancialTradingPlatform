using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DataPipeline.Functions;

public static class DataPipelineMetrics
{
    [Function("GetPipelineMetrics")]
    public static async Task<string> GetPipelineMetrics(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pipeline/metrics")] HttpRequestData req,
        ILogger log)
    {
        log.LogInformation("Fetching pipeline metrics");

        var metrics = new
        {
            timestamp = DateTime.UtcNow,
            recordsProcessed = 0,
            recordsFailed = 0,
            averageProcessingTime = 0.0,
            status = "healthy"
        };

        return System.Text.Json.JsonSerializer.Serialize(metrics);
    }
}
