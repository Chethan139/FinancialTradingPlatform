# Quick Start Guide

## Prerequisites

### Required
- ✅ **Visual Studio 2022** or **VS Code** with C# extension
- ✅ **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- ✅ **Git** - [Download](https://git-scm.com/)

### Optional (for Azure deployment)
- ⚙️ **Azure CLI** - [Download](https://docs.microsoft.com/cli/azure/install-azure-cli)
- ⚙️ **Azure Subscription** - [Free Trial](https://azure.microsoft.com/free/)

---

## Local Development Setup

### Option 1: Docker Compose (Recommended)

**Fastest way to run the entire stack locally**

```bash
# 1. Navigate to project directory
cd FinancialTradingPlatform

# 2. Start all services
docker-compose up -d

# 3. Wait for services to initialize (30-60 seconds)
docker-compose logs -f

# 4. Verify services are running
docker-compose ps
```

**Access Points:**
- Trading Engine API: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`
- All services: See docker-compose.yml for ports

**Stop services:**
```bash
docker-compose down
```

**Clean up (remove volumes):**
```bash
docker-compose down -v
```

---

### Option 2: Run Locally Without Docker

**Requirements:**
- SQL Server 2022 (or Azure SQL Database)
- Azure Cosmos DB Emulator
- Redis

#### Step 1: Install Dependencies

**SQL Server:**
```bash
# Windows: Download from Microsoft
# macOS: Use Docker
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

**Cosmos DB Emulator:**
```bash
# Windows: Install from Microsoft
# macOS/Linux: Use Docker
docker run -p 8081:8081 -p 10251:10251 -p 10252:10252 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

**Redis:**
```bash
docker run -p 6379:6379 -d redis:7-alpine
```

#### Step 2: Update Configuration

Edit `src/Services/TradingEngine.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "TradingDb": "Server=localhost;Database=TradingPlatform;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "CosmosDb": {
    "Endpoint": "https://localhost:8081/",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

#### Step 3: Run Services

```bash
# Terminal 1 - Trading Engine
cd src/Services/TradingEngine.API
dotnet run

# Terminal 2 - Portfolio API
cd src/Services/Portfolio.API
dotnet run

# Terminal 3 - Risk Analysis API
cd src/Services/RiskAnalysis.API
dotnet run
```

---

## Testing the API

### Using Swagger UI

1. Navigate to `https://localhost:5001/swagger`
2. Click "Authorize" button
3. For local testing, you can skip authentication temporarily

### Using Postman

Import the collection:
```bash
# Collection file location
docs/postman/FinancialTradingPlatform.postman_collection.json
```

### Sample API Calls

**Create an Order:**
```bash
POST https://localhost:5001/api/orders
Content-Type: application/json
X-Idempotency-Key: unique-key-123

{
  "portfolioId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "symbol": "AAPL",
  "orderType": "Market",
  "side": "Buy",
  "quantity": 100,
  "timeInForce": "Day"
}
```

**Get Order Status:**
```bash
GET https://localhost:5001/api/orders/{orderId}
```

**Get All Orders:**
```bash
GET https://localhost:5001/api/orders?status=Pending&pageSize=10
```

---

## Database Initialization

### SQL Server

The application will create the database automatically on first run. To run migrations manually:

```bash
cd src/Shared/Infrastructure
dotnet ef database update --startup-project ../../Services/TradingEngine.API
```

### Cosmos DB

Containers are created automatically on first run. See logs:
```
Database TradingPlatform ready
Container Orders created
Container Portfolios created
Container EventStore created
```

---

## Monitoring & Debugging

### Application Logs

**Structured logging with Serilog:**
```bash
# View logs in console
docker-compose logs -f tradingengine

# View logs in Seq (if enabled)
http://localhost:5341
```

### Health Checks

```bash
# Check service health
curl https://localhost:5001/health

# Expected response
{
  "status": "Healthy",
  "results": {
    "sqlserver": "Healthy",
    "redis": "Healthy"
  }
}
```

### Database Inspection

**SQL Server:**
```bash
# Using Azure Data Studio or SSMS
Server: localhost,1433
Username: sa
Password: YourStrong@Password123
```

**Cosmos DB:**
```bash
# Using Data Explorer
https://localhost:8081/_explorer/index.html
```

**Redis:**
```bash
# Using Redis CLI
docker exec -it trading-redis redis-cli -a RedisPassword123
> KEYS *
> GET ratelimit:user123:202403161430
```

---

## Common Issues & Solutions

### Issue: Cosmos DB connection fails
```
Solution: Ensure emulator is running and certificate is trusted
Windows: Run as Administrator
macOS: Use Docker image
```

### Issue: SQL Server connection fails
```
Solution: Check connection string
Verify SQL Server is running: docker ps
Test connection: dotnet ef database update
```

### Issue: Port already in use
```
Solution: Change ports in docker-compose.yml
Or stop conflicting services:
  netstat -ano | findstr :5001
  taskkill /PID <pid> /F
```

### Issue: Redis authentication fails
```
Solution: Check password in appsettings.json matches docker-compose.yml
Default: RedisPassword123
```

---

## Next Steps

1. ✅ **Explore the APIs** - Use Swagger UI to test endpoints
2. ✅ **Review the Code** - Start with `TradingEngine.API/Controllers/OrdersController.cs`
3. ✅ **Read the Docs** - See `/docs` folder for architecture diagrams
4. ✅ **Run Tests** - `dotnet test` in tests folder
5. ✅ **Deploy to Azure** - See `docs/deployment/azure-setup.md`

---

## Development Workflow

### Making Changes

```bash
# 1. Create feature branch
git checkout -b feature/add-stop-loss-orders

# 2. Make changes, test locally
dotnet build
dotnet test

# 3. Commit and push
git add .
git commit -m "Add stop-loss order support"
git push origin feature/add-stop-loss-orders

# 4. Create pull request
```

### Debugging

**Visual Studio:**
1. Set TradingEngine.API as startup project
2. Press F5 to start debugging
3. Set breakpoints in controllers/handlers

**VS Code:**
1. Open `.vscode/launch.json`
2. Select "TradingEngine.API"
3. Press F5

---

## Production Deployment

See detailed guides in `/docs/deployment/`:
- `azure-setup.md` - Azure resource provisioning
- `ci-cd-pipeline.md` - GitHub Actions workflow
- `terraform.md` - Infrastructure as Code

**Quick Deploy to Azure:**
```bash
# 1. Login to Azure
az login

# 2. Run deployment script
./infrastructure/scripts/deploy-to-azure.sh

# 3. Monitor deployment
az deployment group show -g rg-trading-platform -n main-deployment
```

---

## Support & Troubleshooting

- 📖 **Documentation:** `/docs` folder
- 🐛 **Issues:** GitHub Issues
- 💬 **Discussions:** GitHub Discussions
- 📧 **Email:** [Your contact]

---

## License

MIT License - See LICENSE file for details

---

**Happy coding! 🚀**
