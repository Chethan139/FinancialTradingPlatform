using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Runtime;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/order-processing-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .UseSerilog()
        .Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order Processing Functions terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
