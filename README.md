# Azure Financial Trading Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-Cloud-blue)](https://azure.microsoft.com/)
[![Microservices](https://img.shields.io/badge/Architecture-Microservices-green)](https://microservices.io/)

## 🏆 Enterprise-Grade Financial Trading Platform

A production-ready, cloud-native financial trading platform showcasing modern microservices architecture, event-driven design, and Azure cloud services.

## 🎯 Project Purpose

This project demonstrates enterprise-level software architecture skills for:
- **Technical Interviews**: Comprehensive implementation of design patterns and best practices
- **Portfolio Showcase**: Real-world application of microservices, CQRS, Event Sourcing
- **Azure Expertise**: Full utilization of Azure services (Service Bus, Event Hubs, Cosmos DB, Functions, etc.)
- **Scalability & Resilience**: Rate limiting, circuit breakers, distributed tracing

## 🏗️ Architecture Overview

### Microservices
1. **Trading Engine API** - Order management and execution
2. **Portfolio API** - Portfolio tracking and valuation
3. **Risk Analysis API** - Pre-trade and post-trade risk assessment
4. **Market Data API** - Real-time market data streaming
5. **Reporting API** - Analytics and reporting
6. **Notification API** - Multi-channel notifications

### Azure Functions
- **Order Processing Functions** - Durable Functions for order workflows
- **Data Pipeline Functions** - ETL using Azure Data Factory triggers

### Infrastructure
- **API Gateway** - Unified entry point with rate limiting
- **Azure Service Bus** - Event-driven communication (topics & queues)
- **Azure Event Hubs** - High-throughput market data streaming
- **Azure Cosmos DB** - Global distribution for market data
- **SQL Server** - Transactional consistency for orders/portfolios
- **Redis Cache** - Distributed caching and rate limiting
- **Application Insights** - Distributed tracing and monitoring

## 🎨 Design Patterns & Architecture

### CQRS (Command Query Responsibility Segregation)
- Separate read and write models
- Optimized queries for different use cases
- Event sourcing for complete audit trail

### Event-Driven Architecture
- **Service Bus**: Guaranteed delivery for commands/events
- **Event Hubs**: High-throughput streaming for market data
- **Event Sourcing**: Complete history of state changes

### Resilience Patterns
- **Circuit Breaker** - Prevent cascade failures (Polly)
- **Retry with Exponential Backoff** - Handle transient failures
- **Rate Limiting** - Token bucket algorithm for API protection
- **Bulkhead Isolation** - Resource isolation between services

### Database Patterns
- **Unit of Work** - Transaction management
- **Repository Pattern** - Data access abstraction
- **Optimistic Concurrency** - Row versioning for conflict detection
- **Soft Delete** - Logical deletion with audit trail

## 🚀 Key Features

### High Availability & Fault Tolerance
✅ Circuit breaker prevents cascade failures  
✅ Retry policies with exponential backoff and jitter  
✅ Health checks for all services  
✅ Dead letter queues for failed messages  

### Scalability & Performance
✅ Distributed caching with Redis  
✅ Rate limiting (100-10000 req/min based on tier)  
✅ Cosmos DB for <10ms latency at scale  
✅ Event Hubs for millions of events/second  
✅ Horizontal scaling with Azure App Service  

### Security
✅ Azure AD/Entra ID authentication  
✅ JWT token validation  
✅ Role-based access control (RBAC)  
✅ Secrets management with Azure Key Vault  

### Observability
✅ Distributed tracing with Application Insights  
✅ Correlation IDs across services  
✅ Structured logging with Serilog  
✅ Custom metrics and alerts  

## 📋 Tech Stack

### Backend
- **.NET 8** - Latest LTS version
- **C# 12** - Latest language features
- **ASP.NET Core** - Web APIs
- **Entity Framework Core 8** - ORM for SQL Server
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **Polly** - Resilience policies

### Azure Services
- **Azure Service Bus** - Messaging
- **Azure Event Hubs** - Event streaming
- **Azure Cosmos DB** - NoSQL database
- **Azure SQL Database** - Relational database
- **Azure Functions** - Serverless compute
- **Azure Durable Functions** - Workflow orchestration
- **Azure Data Factory** - ETL pipelines
- **Azure Application Insights** - APM
- **Azure Key Vault** - Secrets management
- **Azure Redis Cache** - Distributed cache

### DevOps
- **Docker** - Containerization
- **Docker Compose** - Local development
- **GitHub Actions** - CI/CD
- **Terraform** - Infrastructure as Code

## 🛠️ Getting Started

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- Azure Subscription (for cloud deployment)
- Visual Studio 2022 or VS Code

### Local Development
```bash
# Clone repository
git clone https://github.com/Chethan139/FinancialTradingPlatform.git
cd FinancialTradingPlatform

# Run with Docker Compose
docker-compose up -d

# Or run individual service
cd src/Services/TradingEngine.API
dotnet run
```

### Configuration
Update `appsettings.Development.json` with your connection strings:
```json
{
  "ConnectionStrings": {
    "TradingDb": "Server=localhost;Database=TradingDb;...",
    "CosmosDb": "AccountEndpoint=https://...;AccountKey=...",
    "ServiceBus": "Endpoint=sb://...;SharedAccessKeyName=...",
    "EventHubs": "Endpoint=sb://...;SharedAccessKeyName=...",
    "Redis": "localhost:6379"
  }
}
```

## 📊 API Endpoints

### Trading Engine API
- `POST /api/orders` - Create order
- `GET /api/orders/{id}` - Get order details
- `PUT /api/orders/{id}/cancel` - Cancel order
- `GET /api/orders/user/{userId}` - Get user orders

### Portfolio API
- `GET /api/portfolios/{id}` - Get portfolio
- `GET /api/portfolios/{id}/positions` - Get positions
- `GET /api/portfolios/{id}/performance` - Get performance metrics

### Market Data API
- `GET /api/marketdata/{symbol}/quote` - Get real-time quote
- `GET /api/marketdata/{symbol}/history` - Get historical data
- `WebSocket /ws/marketdata` - Real-time market data stream

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📚 Interview Questions Covered

This project addresses common interview questions:

### Architecture
- How do you design a microservices architecture?
- How do you handle distributed transactions?
- What is CQRS and when would you use it?
- How do you implement event sourcing?

### Scalability
- How do you handle high traffic (rate limiting, caching)?
- How do you scale databases (read replicas, sharding)?
- How do you handle millions of events per second (Event Hubs)?

### Reliability
- How do you prevent cascade failures (circuit breaker)?
- How do you handle transient failures (retry policies)?
- How do you ensure exactly-once processing (idempotency)?

### Data Consistency
- How do you maintain consistency in distributed systems?
- What is optimistic concurrency control?
- How do you handle eventual consistency?

See [docs/interview-qa/README.md](docs/interview-qa/README.md) for detailed Q&A.

## 📖 Documentation

- [Architecture Diagrams](docs/architecture/) - C4 model, sequence diagrams
- [API Documentation](docs/api/) - OpenAPI/Swagger specs
- [Deployment Guide](docs/deployment/) - Azure deployment steps
- [Interview Q&A](docs/interview-qa/) - Common interview questions

## 🤝 Contributing

This is a portfolio project. Feedback and suggestions are welcome!

## 📄 License

MIT License - See [LICENSE](LICENSE) for details

## 👤 Author

**Chethan**
- GitHub: [@Chethan139](https://github.com/Chethan139)
- LinkedIn: [Add your LinkedIn]

---

⭐ **Star this repo if you find it helpful for your interviews!**
