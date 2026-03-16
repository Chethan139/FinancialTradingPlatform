#!/usr/bin/env python3
import os
from pathlib import Path

BASE = Path("/home/claude/FinancialTradingPlatform")

# Docker Compose
(BASE / "docker-compose.yml").write_text('''version: '3.8'

services:
  # SQL Server
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  # Redis
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  # Trading Engine
  trading-engine:
    build:
      context: .
      dockerfile: src/Services/TradingEngine.API/Dockerfile
    ports:
      - "5001:80"
    environment:
      - ConnectionStrings__TradingDb=Server=sqlserver;Database=TradingDb;User=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - sqlserver
      - redis

volumes:
  sqldata:
''')

# appsettings.json for Trading Engine
(BASE / "src/Services/TradingEngine.API/appsettings.json").write_text('''{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "TradingDb": "Server=localhost;Database=TradingDb;Integrated Security=true;TrustServerCertificate=True",
    "CosmosDb": "AccountEndpoint=https://YOUR_ACCOUNT.documents.azure.com:443/;AccountKey=YOUR_KEY",
    "ServiceBus": "Endpoint=sb://YOUR_NAMESPACE.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY",
    "EventHubs": "Endpoint=sb://YOUR_NAMESPACE.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY",
    "Redis": "localhost:6379"
  },
  "CosmosDb": {
    "Endpoint": "https://YOUR_ACCOUNT.documents.azure.com:443/",
    "Key": "YOUR_KEY",
    "DatabaseName": "TradingData",
    "ContainerName": "MarketData"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "YOUR_DOMAIN.onmicrosoft.com",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=YOUR_KEY;IngestionEndpoint=https://YOUR_REGION.in.applicationinsights.azure.com/"
  },
  "AllowedHosts": "*"
}
''')

# Dockerfile for Trading Engine
(BASE / "src/Services/TradingEngine.API/Dockerfile").write_text('''FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/TradingEngine.API/TradingEngine.API.csproj", "src/Services/TradingEngine.API/"]
COPY ["src/Shared/Common/Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/EventContracts/EventContracts.csproj", "src/Shared/EventContracts/"]
COPY ["src/Shared/Infrastructure/Infrastructure.csproj", "src/Shared/Infrastructure/"]
RUN dotnet restore "src/Services/TradingEngine.API/TradingEngine.API.csproj"
COPY . .
WORKDIR "/src/src/Services/TradingEngine.API"
RUN dotnet build "TradingEngine.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TradingEngine.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TradingEngine.API.dll"]
''')

# .gitignore
(BASE / ".gitignore").write_text('''## .NET
bin/
obj/
*.user
*.suo
*.userprefs
.vs/
.vscode/
*.dll
*.pdb

## Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Bb]in/
[Oo]bj/

## NuGet
*.nupkg
**/packages/*
!**/packages/build/

## Docker
.dockerignore

## Sensitive data
appsettings.Development.json
appsettings.*.json
*.pfx
*.key
secrets.json

## OS
.DS_Store
Thumbs.db
''')

# GitHub Actions CI/CD
(BASE / ".github/workflows/ci-cd.yml").write_text('''name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --configuration Release
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
''')

# Interview Q&A Document
os.makedirs(BASE / "docs/interview-qa", exist_ok=True)
(BASE / "docs/interview-qa/README.md").write_text('''# Interview Questions & Answers

## Architecture & Design Patterns

### Q: Why did you choose microservices architecture?
**A:** Microservices provide several benefits for this trading platform:
- **Independent Scaling**: Trading engine can scale separately from reporting
- **Technology Diversity**: Can use optimal tech stack per service
- **Fault Isolation**: Failure in reporting doesn't affect trade execution
- **Team Autonomy**: Different teams can own different services
- **Deployment Independence**: Deploy changes to one service without affecting others

### Q: Explain your implementation of CQRS
**A:** CQRS separates read and write operations:
- **Commands** (writes): Create/modify state (CreateOrderCommand)
- **Queries** (reads): Fetch data without side effects (GetOrderQuery)
- **Benefits**:
  - Optimized read models for specific use cases
  - Independent scaling of reads vs writes
  - Event sourcing for complete audit trail
  - Better security (separate permissions for reads/writes)

### Q: How do you handle distributed transactions?
**A:** Used Saga pattern with choreography:
1. Order created → OrderCreatedEvent published
2. Risk service validates → OrderValidatedEvent published
3. Portfolio service reserves funds → FundsReservedEvent published
4. If any step fails → compensating transactions triggered
- **Advantages**: No distributed locks, better scalability
- **Trade-offs**: Eventual consistency, complexity

### Q: Why both Cosmos DB and SQL Server?
**A:** Each for different use cases:
- **Cosmos DB**: Market data, real-time prices
  - <10ms latency globally
  - Automatic indexing
  - Multi-region writes
- **SQL Server**: Orders, portfolios, transactions
  - ACID guarantees
  - Complex joins and reporting
  - Strong consistency

## Scalability & Performance

### Q: How do you handle high traffic?
**A:** Multiple strategies:
1. **Rate Limiting**: Token bucket (100-10k req/min per tier)
2. **Caching**: Redis for frequently accessed data
3. **Circuit Breaker**: Fail fast on unhealthy services
4. **Horizontal Scaling**: Stateless services, scale out
5. **CDN**: Static content delivery
6. **Database Read Replicas**: Distribute read load

### Q: How does your rate limiting work?
**A:** Implemented token bucket algorithm:
```
- Tokens added at constant rate (e.g., 1000/min)
- Each request consumes one token
- If no tokens available → 429 Too Many Requests
- Distributed across instances using Redis
- Different limits per tier (anonymous/authenticated/premium)
```

### Q: How do you handle database scaling?
**A:** Multiple approaches:
1. **Vertical**: Larger instance for SQL Server
2. **Horizontal**: Read replicas for queries
3. **Partitioning**: Cosmos DB auto-partitions by symbol
4. **Caching**: Redis reduces database hits
5. **CQRS**: Separate read database optimized for queries

## Reliability & Resilience

### Q: Explain your circuit breaker implementation
**A:** Using Polly library:
- **Closed**: Normal operation, requests pass through
- **Open**: After 5 failures → fail fast for 30s
- **Half-Open**: Test one request, close if succeeds
- **Benefits**: Prevents cascade failures, gives time to recover

### Q: How do you ensure exactly-once processing?
**A:** Multiple mechanisms:
1. **Idempotency Keys**: Unique key per order prevents duplicates
2. **Database Constraints**: Unique index on idempotency key
3. **Service Bus**: Duplicate detection enabled
4. **Optimistic Concurrency**: Row versioning prevents conflicts

### Q: How do you handle service failures?
**A:** Defense in depth:
1. **Retry Policies**: Exponential backoff with jitter
2. **Circuit Breakers**: Prevent cascade failures
3. **Timeouts**: Prevent indefinite hangs
4. **Health Checks**: Kubernetes removes unhealthy pods
5. **Dead Letter Queues**: Preserve failed messages

## Data Consistency

### Q: How do you maintain consistency in distributed system?
**A:** Eventual consistency with compensating transactions:
- Events published to Service Bus (at-least-once)
- Each service processes events idempotently
- If processing fails → retry with exponential backoff
- If max retries exceeded → dead letter queue
- Manual intervention for dead lettered messages

### Q: What is optimistic concurrency?
**A:** Prevents lost updates without locks:
```csharp
// Entity has RowVersion (timestamp)
var order = await _db.Orders.FindAsync(id);
order.Status = OrderStatus.Filled;
await _db.SaveChangesAsync(); 
// If RowVersion changed → DbUpdateConcurrencyException
// Retry with fresh data
```

## Azure Services

### Q: Why Service Bus over Event Hubs?
**A:** Different use cases:
- **Service Bus**: Commands/events requiring guaranteed delivery
  - Transactions, sessions, dead letter queue
  - Lower throughput (<10k msg/sec)
- **Event Hubs**: High-throughput streaming (millions/sec)
  - Market data feeds, telemetry
  - No guaranteed delivery, partition-based

### Q: How do you use Azure Functions?
**A:** Two types implemented:
1. **Regular Functions**: Event-triggered processing
   - Service Bus trigger → process OrderCreatedEvent
2. **Durable Functions**: Complex workflows
   - Saga orchestration for multi-step order flow
   - Human approval workflows

## Security

### Q: How is authentication handled?
**A:** Azure AD/Entra ID with JWT tokens:
```
1. User authenticates with Azure AD
2. Receives JWT access token
3. Includes token in API requests (Bearer)
4. API validates token signature and claims
5. Extracts user identity and roles
6. Enforces role-based access control
```

## Monitoring & Observability

### Q: How do you monitor the system?
**A:** Multi-layer approach:
1. **Application Insights**: Distributed tracing, exceptions
2. **Correlation IDs**: Track requests across services
3. **Structured Logging**: Serilog with context enrichment
4. **Custom Metrics**: Business metrics (orders/sec, P&L)
5. **Health Checks**: Kubernetes liveness/readiness probes
6. **Alerts**: PagerDuty integration for critical issues
''')

print("✅ Project generation complete!")
print(f"📁 Location: {BASE}")
print("\\n🚀 Next steps:")
print("1. Update appsettings.json with your Azure connection strings")
print("2. Run: dotnet restore")
print("3. Run: docker-compose up -d")
print("4. Navigate to http://localhost:5001 for Swagger UI")
