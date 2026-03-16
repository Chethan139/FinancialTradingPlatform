using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json.Serialization;

namespace OrderProcessing.Functions;

public class OrderOrchestrator
{
    [Function("StartOrderProcessing")]
    public async Task<HttpResponseData> StartOrderProcessing(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/process")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var requestBody = await req.Content.ReadAsStringAsync();
        var orderRequest = System.Text.Json.JsonSerializer.Deserialize<OrderRequest>(requestBody);

        if (orderRequest == null)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { message = "Invalid request" });
            return badResponse;
        }

        var orderId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(OrderProcessingOrchestrator),
            new OrderProcessingInput { OrderId = orderId, CorrelationId = correlationId, UserId = orderRequest.UserId, Symbol = orderRequest.Symbol, Quantity = orderRequest.Quantity, Price = orderRequest.Price });

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId, orderId, correlationId, message = "Order processing started" });
        response.Headers.Add("X-Correlation-ID", correlationId);
        return response;
    }

    [Function("GetOrderStatus")]
    public async Task<HttpResponseData> GetOrderStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{instanceId}/status")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var status = await client.GetInstanceAsync(instanceId);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { instanceId, status = status?.RuntimeStatus.ToString() });
        return response;
    }
}

[DurableTask(nameof(OrderProcessingOrchestrator))]
public static async Task RunOrderProcessingOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context, OrderProcessingInput input)
{
    context.SetCustomStatus($"Processing order {input.OrderId}");
    try
    {
        await context.CallActivityAsync(nameof(CreateOrderActivity), input);
        await context.CallActivityAsync(nameof(ReserveFundsActivity), input);
        await context.CallActivityAsync(nameof(ValidateRiskActivity), input);
        await context.CallActivityAsync(nameof(ExecuteTradeActivity), input);
        context.SetCustomStatus($"Order {input.OrderId} completed");
    }
    catch (Exception ex)
    {
        context.SetCustomStatus($"Failed: {ex.Message}");
        throw;
    }
}

[Function(nameof(CreateOrderActivity))]
public static async Task CreateOrderActivity([ActivityTrigger] OrderProcessingInput input, ILogger log)
{
    log.LogInformation("Creating order: {OrderId}", input.OrderId);
    await Task.Delay(100);
}

[Function(nameof(ReserveFundsActivity))]
public static async Task ReserveFundsActivity([ActivityTrigger] OrderProcessingInput input, ILogger log)
{
    log.LogInformation("Reserving funds");
    await Task.Delay(100);
}

[Function(nameof(ValidateRiskActivity))]
public static async Task ValidateRiskActivity([ActivityTrigger] OrderProcessingInput input, ILogger log)
{
    log.LogInformation("Validating risk");
    await Task.Delay(100);
}

[Function(nameof(ExecuteTradeActivity))]
public static async Task ExecuteTradeActivity([ActivityTrigger] OrderProcessingInput input, ILogger log)
{
    log.LogInformation("Executing trade");
    await Task.Delay(100);
}

public class OrderProcessingInput
{
    [JsonPropertyName("orderId")] public string OrderId { get; set; }
    [JsonPropertyName("correlationId")] public string CorrelationId { get; set; }
    [JsonPropertyName("userId")] public string UserId { get; set; }
    [JsonPropertyName("symbol")] public string Symbol { get; set; }
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    [JsonPropertyName("price")] public decimal Price { get; set; }
}

public class OrderRequest
{
    [JsonPropertyName("userId")] public string UserId { get; set; }
    [JsonPropertyName("symbol")] public string Symbol { get; set; }
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    [JsonPropertyName("price")] public decimal Price { get; set; }
}
