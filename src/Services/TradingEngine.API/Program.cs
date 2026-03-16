using Infrastructure.Data.Contexts;
using Infrastructure.Messaging.ServiceBus;
using Infrastructure.Azure.CosmosDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;
using Serilog;
using Common.Resilience;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "TradingEngine")
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        builder.Configuration["ApplicationInsights:ConnectionString"],
        TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();

// Add Azure AD Authentication with JWT Bearer
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTraderRole", policy =>
        policy.RequireRole("Trader", "Admin"));
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
});

// Add SQL Server for transactional data
builder.Services.AddDbContext<TradingDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TradingDb"),
        sqlOptions =>
        {
            // Enable retry on failure for transient errors
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            
            // Command timeout for long-running queries
            sqlOptions.CommandTimeout(30);
        });
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add Cosmos DB for high-throughput market data
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var endpoint = builder.Configuration["CosmosDb:Endpoint"];
    var key = builder.Configuration["CosmosDb:Key"];
    
    return new CosmosClient(endpoint, key, new CosmosClientOptions
    {
        ApplicationName = "TradingEngine",
        ConnectionMode = ConnectionMode.Direct, // Better performance
        ConsistencyLevel = ConsistencyLevel.Session, // Balance of consistency and performance
        MaxRetryAttemptsOnRateLimitedRequests = 5,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
    });
});

// Add Azure Service Bus
builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("ServiceBus");
    return new ServiceBusClient(connectionString);
});

builder.Services.AddSingleton<ServiceBusPublisher>();

// Add Redis for distributed caching and rate limiting
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "TradingEngine:";
});

// Add distributed memory cache as fallback
builder.Services.AddDistributedMemoryCache();

// Add resilience policies
builder.Services.AddSingleton<ResiliencePolicies>();

// Add MediatR for CQRS pattern
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Trading Engine API", 
        Version = "v1",
        Description = "Microservice for order management and trade execution",
        Contact = new() { Name = "Chethan", Url = new Uri("https://github.com/Chethan139") }
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TradingDbContext>("sql-server")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, "redis")
    .AddAzureServiceBusTopic(
        builder.Configuration.GetConnectionString("ServiceBus")!,
        "order-events",
        "service-bus");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading Engine API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();
