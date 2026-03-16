# Financial Trading Platform - Complete Project Explanation for Freshers

## 🎯 Project Overview

**What is this project?**
This is a **large-scale, enterprise-grade financial trading platform** built on **Microsoft Azure** using **microservices architecture**. It allows users to buy/sell stocks, manage portfolios, and track market data in real-time with high performance and reliability.

**Why is it important?**
- Handles real-time stock trading with microsecond latency requirements
- Processes thousands of concurrent orders without errors
- Must be extremely reliable (trading platforms are mission-critical)
- Demonstrates advanced software architecture patterns used in fintech companies

---

## 📊 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      CLIENT APPLICATIONS                         │
│              (Web Browser / Mobile / Trading Terminals)          │
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       │ HTTPS Requests
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                    API GATEWAY (Reverse Proxy)                   │
│  - Routes requests to right microservice                         │
│  - Handles authentication/authorization                          │
│  - Rate limiting (prevent abuse)                                 │
│  - Load balancing                                                │
└──────────────────────┬──────────────────────────────────────────┘
                       │
        ┌──────────────┼──────────────┬──────────────┐
        ▼              ▼              ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ MarketData   │ │  Order       │ │ Portfolio    │ │ Notification │
│ Service      │ │ Processing   │ │ Service      │ │ Service      │
│              │ │              │ │              │ │              │
│ - Real-time  │ │ - Buy/Sell   │ │ - Holdings   │ │ - Email      │
│   prices     │ │ - Validating │ │ - Balance    │ │ - SMS        │
│ - Stocks info│ │ - Risk check │ │ - P&L        │ │ - Push notif │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
        │              │              │              │
        │              │              │              │
        └──────────────┼──────────────┼──────────────┘
                       │
         ┌─────────────┴─────────────┐
         ▼                           ▼
    ┌─────────────┐          ┌──────────────┐
    │  Cosmos DB  │          │ SQL Server   │
    │  (NoSQL)    │          │ (Relational) │
    │             │          │              │
    │ - Market    │          │ - Orders     │
    │   data      │          │ - Users      │
    │ - Prices    │          │ - Portfolios │
    │ - Real-time │          │ - Audit log  │
    └─────────────┘          └──────────────┘
         │
         ▼
    ┌─────────────┐
    │ Service Bus │
    │ (Azure)     │
    │             │
    │ - Event     │
    │   messaging │
    │ - Async     │
    │   communication
    └─────────────┘
```

---

## 🏗️ Solution Structure Breakdown

### **What are these 9 projects?**

The Visual Studio solution contains 9 projects (though only some are visible):

#### **1. Gateway / API Gateway** (API.Gateway)
**What it does:** Acts as a "front desk" for the entire application
- All client requests go through here first
- Directs requests to the right microservice
- Handles user authentication (checking if user is logged in)
- Rate limiting (prevents one user from making 1 million requests)
- Load balancing (distributes traffic)

**Real-world analogy:** Like a receptionist who directs visitors to the right department

---

#### **2. Services**

##### **A. MarketData.API**
**Purpose:** Provides real-time stock market data

**What it does:**
- Fetches current stock prices from exchanges
- Stores prices in Cosmos DB for fast access
- Provides endpoints like:
  - `GET /api/stocks/{symbol}/price` → Get Apple stock price
  - `GET /api/stocks/top-gainers` → Get top performing stocks

**Why it's separate:** 
- Market data updates continuously (needs independent scaling)
- Different tech stack optimized for read-heavy operations
- Can fail independently without affecting trading

**Database:** Cosmos DB (NoSQL)
- Why? Real-time prices, need sub-10ms latency globally
- Auto-scales with demand

---

##### **B. Order Processing Service** (Not shown but in architecture)
**Purpose:** Handles buy/sell orders

**What it does:**
1. Receives a user's order "Buy 10 Apple stocks at $150"
2. Validates the order:
   - Does user have enough money?
   - Is the price reasonable?
   - Is the order within trading hours?
3. Applies business rules:
   - Risk checks (don't let one order exceed 30% of portfolio)
   - Compliance checks
4. Executes the order
5. Updates user's portfolio

**Pattern used:** CQRS (Command Query Responsibility Segregation)
```
Command (Write): "CreateOrderCommand" → Processes the order
Query (Read): "GetOrderQuery" → Retrieves order details
```

This separation allows:
- Independent optimization for reads vs writes
- Scaling read and write separately
- Better audit trail

---

##### **C. Portfolio Service** (Not shown but in architecture)
**Purpose:** Manages user's stock holdings

**What it does:**
- Tracks what stocks user owns
- Calculates current portfolio value
- Shows profit/loss (P&L)
- Tracks cash balance
- Shows portfolio breakdown (what % in each stock)

**Database:** SQL Server (Relational)
- Why? Need ACID guarantees (exact balance must be correct)
- Complex reporting queries

---

##### **D. Notification Service**
**Purpose:** Sends alerts to users

**What it does:**
- Sends email when order is executed
- SMS when stock price hits user's alert level
- Push notifications on mobile app
- Real-time websocket updates

**How it works:**
1. When an order is executed in Order Processing service
2. An event is published to Service Bus
3. Notification service listens to this event
4. Sends appropriate notification to user

---

#### **3. Functions** (Azure Functions - Serverless)

##### **A. DataPipeline.Functions**
**Purpose:** Processes market data in the background

**What it does:**
- Fetches stock data from external exchanges (NYSE, NASDAQ, etc.)
- Cleans and transforms data
- Stores in Cosmos DB
- Runs on a schedule (every 5 seconds for example)

**Why serverless functions?**
- No need to manage servers
- Pay only when code runs
- Scales automatically

##### **B. OrderProcessing.Functions**
**Purpose:** Complex, multi-step order workflows

**What it does:**
- Implements "Saga pattern" for distributed transactions
- Example workflow:
  ```
  1. Create order in database
  2. Reserve funds from account
  3. Check risk limits
  4. Check compliance
  5. Execute trade
  (If any step fails → undo previous steps)
  ```

**Why Durable Functions?**
- Can handle steps that take minutes or hours
- Retry automatically if step fails
- No loss of state if system crashes

---

#### **4. GitHub Actions**
**Purpose:** Automated deployment (CI/CD)

**What it does:**
- When developer pushes code to GitHub
- Automatically runs tests
- If tests pass → builds the application
- Deploys to Azure automatically

---

## 🗄️ Databases Explained

### **SQL Server** (Relational Database)
**Think of it as:** Structured spreadsheets with relationships

```
┌─────────────────────────────────────────┐
│           ORDERS Table                  │
├─────────────────────────────────────────┤
│ OrderID │ UserID │ StockSymbol │ Price │
├─────────────────────────────────────────┤
│    1    │   101  │    AAPL     │ 150   │
│    2    │   102  │    GOOGL    │ 2500  │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│           USERS Table                   │
├─────────────────────────────────────────┤
│ UserID │ Name    │ Email               │
├─────────────────────────────────────────┤
│   101  │ Alice   │ alice@email.com     │
│   102  │ Bob     │ bob@email.com       │
└─────────────────────────────────────────┘
```

**Used for:**
- Orders (buy/sell records)
- User accounts
- Portfolio holdings
- Audit logs
- Transactions

**Guarantees:**
- **ACID**: Atomicity, Consistency, Isolation, Durability
- If money is deducted, order MUST be created
- No partial transactions

---

### **Cosmos DB** (NoSQL Database)
**Think of it as:** Flexible JSON documents

```json
{
  "id": "AAPL",
  "symbol": "AAPL",
  "price": 150.25,
  "timestamp": "2024-03-16T10:30:00Z",
  "dailyChange": 2.5,
  "volume": 50000000
}
```

**Used for:**
- Real-time stock prices
- Market data
- High-frequency data updates

**Advantages:**
- Super fast reads (<10ms)
- Auto-scales globally
- No schema required (flexible)

---

## 🔄 How A Trade Happens (End-to-End Flow)

### **Scenario: Alice buys 10 Apple stocks at $150**

```
Step 1: Client Request
┌──────────────────────────────────────────────────────────┐
│ Alice opens the trading app on her phone                 │
│ Enters: Buy 10 AAPL @ $150                               │
│ Clicks "BUY"                                             │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
Step 2: API Gateway
┌──────────────────────────────────────────────────────────┐
│ Request reaches API Gateway                              │
│ - Verifies Alice is logged in (JWT token)                │
│ - Checks rate limiting (Alice hasn't exceeded limit)     │
│ - Routes to Order Processing Service                     │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
Step 3: Order Processing Service
┌──────────────────────────────────────────────────────────┐
│ Receives order: "BUY 10 AAPL @ $150"                     │
│                                                          │
│ Step 3a: Validate                                        │
│ - Check if AAPL is valid stock ✓                         │
│ - Check if Alice has $1500 in cash ✓                     │
│ - Check trading hours ✓                                  │
│                                                          │
│ Step 3b: Risk Check                                      │
│ - Alice's current portfolio: $10,000                     │
│ - This order: $1,500 (15% of portfolio) ✓                │
│ - Max allowed: 30% ✓                                     │
│                                                          │
│ Step 3c: Create Order in Database                        │
│ - Status: PENDING                                        │
│ - Idempotency Key: Unique identifier to prevent dupes    │
│                                                          │
│ Step 3d: Reserve Funds                                   │
│ - Deduct $1,500 from Alice's cash balance                │
│ - Update SQL Server                                      │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
Step 4: Publish Event to Service Bus
┌──────────────────────────────────────────────────────────┐
│ Event: "OrderCreatedEvent"                               │
│ {                                                        │
│   "OrderID": 12345,                                      │
│   "UserID": 101,                                         │
│   "Symbol": "AAPL",                                      │
│   "Quantity": 10,                                        │
│   "Price": 150,                                          │
│   "Timestamp": "2024-03-16T10:30:00Z"                    │
│ }                                                        │
│                                                          │
│ Service Bus ensures this message is delivered            │
│ even if services are temporarily down                    │
└──────────────────────────┬───────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
Step 5: Multiple Services React to Event

┌─────────────────────────┐
│ Notification Service    │
│ - Sends email to Alice  │
│ "Your order for 10 AAPL │
│  has been created"      │
└─────────────────────────┘

┌─────────────────────────┐
│ Portfolio Service       │
│ - Updates Alice's       │
│   holdings to show      │
│   pending 10 AAPL       │
└─────────────────────────┘

┌─────────────────────────┐
│ Risk Service            │
│ - Validates order       │
│   against risk limits   │
│ - If OK → publishes     │
│   OrderValidatedEvent   │
└─────────────────────────┘

Step 6: Execute Trade
┌──────────────────────────────────────────────────────────┐
│ Once all validations pass:                               │
│ - Order status changes: PENDING → FILLED                 │
│ - Match order against market                             │
│ - Update SQL Server                                      │
│ - Publish "OrderFilledEvent"                             │
└──────────────────────────┬───────────────────────────────┘
                           │
                           ▼
Step 7: Final Notification
┌──────────────────────────────────────────────────────────┐
│ Notification Service receives OrderFilledEvent           │
│ Sends to Alice:                                          │
│ - Email: "Order filled! 10 AAPL @ $150"                  │
│ - Push notification on phone                            │
│ - Updates portfolio value in real-time                   │
└──────────────────────────────────────────────────────────┘

Step 8: Portfolio Updated
┌──────────────────────────────────────────────────────────┐
│ Alice's Portfolio:                                       │
│ Cash: $10,000 → $8,500                                   │
│ Holdings: +10 AAPL (current value: $1500)                │
│ Total Value: $10,000 → $10,000 (no gain/loss yet)        │
│ P&L: $0 (if prices haven't changed)                      │
└──────────────────────────────────────────────────────────┘
```

---

## 🛡️ Key Architectural Patterns

### **1. Microservices Architecture**
**What is it?**
Instead of one big application, break it into small independent services.

**Benefits:**
- Each service can use different technology
- Team can work on different services independently
- If one service crashes, others still work
- Scale individual services based on load

**Drawback:** Increased complexity (managing 4+ services)

---

### **2. CQRS (Command Query Responsibility Segregation)**
**What is it?**
Separate code that writes data from code that reads data.

```
Write Side (Commands)              Read Side (Queries)
  ↓                                  ↓
CreateOrderCommand            GetOrderQuery
UpdatePortfolioCommand        GetPortfolioQuery
  ↓                                  ↓
SQL Server (Write)            Read Replica DB
(Optimized for ACID)          (Optimized for Speed)
```

**Example:**
```csharp
// COMMAND - Write operation
CreateOrderCommand cmd = new CreateOrderCommand
{
    Symbol = "AAPL",
    Quantity = 10,
    Price = 150
};
await commandBus.Send(cmd);

// QUERY - Read operation
GetOrderQuery query = new GetOrderQuery { OrderID = 123 };
var order = await queryBus.Execute(query);
```

---

### **3. Saga Pattern (Distributed Transactions)**
**What is it?**
In a distributed system, you can't use traditional transactions across databases. Instead, use sagas.

```
Step 1: Create Order
       ↓ OrderCreatedEvent
Step 2: Validate Risk
       ↓ if OK → OrderValidatedEvent
       ↓ if FAIL → OrderRejectedEvent → Undo Step 1
Step 3: Reserve Funds
       ↓ if OK → FundsReservedEvent
       ↓ if FAIL → Undo Step 2 and 1 (Compensating Transactions)
Step 4: Execute Trade
```

**Key concept:** If anything fails, undo all previous steps (called compensating transactions)

---

### **4. Event-Driven Architecture**
**What is it?**
Services communicate through events published to a message bus.

```
Order Service publishes → "OrderCreatedEvent" → Service Bus
                                                      ↓
                    ┌─────────────────────────────────┼─────────────────────────────────┐
                    ▼                                  ▼                                  ▼
            Notification Service            Portfolio Service              Risk Service
            (sends email)                    (updates holdings)            (validates)
```

**Why?**
- Loose coupling (services don't know about each other)
- Asynchronous (doesn't have to be instant)
- Can retry if service is down

---

## 🔐 Security Implementation

### **Authentication (Verifying WHO you are)**
```
Alice logs in with username/password
           ↓
Azure AD verifies credentials
           ↓
Issues JWT Token
{
  "sub": "alice@email.com",
  "roles": ["trader"],
  "exp": "2024-03-16T18:30:00Z"
}
           ↓
Alice includes token in API requests:
Authorization: Bearer <jwt-token>
           ↓
API validates token signature
           ↓
If valid, process request
If invalid, return 401 Unauthorized
```

### **Authorization (Verifying WHAT you can do)**
- Alice (role: trader) can create orders
- Bob (role: viewer) can only view prices
- Charlie (role: admin) can access all data

---

## 📊 Data Consistency Strategy

**Problem:** In a microservices system, how do we ensure data is consistent?

**Answer:** Eventual Consistency with Compensating Transactions

```
Scenario: Alice places order, system crashes during fund reservation

Time 1: Order created in DB
Time 2: Funds deducted from account
Time 3: System crashes ❌
Time 4: Service recovers
        Risk service notices order without FundsReservedEvent
Time 5: Triggers compensating transaction
        Adds funds back to account
Time 6: System is consistent again ✓

Why this works:
- Events stored in Service Bus at-least-once
- Retry logic with exponential backoff
- Dead letter queue for persistent failures
- Manual intervention if needed
```

---

## ⚡ Performance & Scalability

### **Rate Limiting (Token Bucket Algorithm)**
```
Imagine Alice has a bucket with 1000 tokens.
Each API request costs 1 token.

1st request:  999 tokens left
2nd request:  998 tokens left
...
1000th request: 0 tokens left
1001st request: ❌ 429 Too Many Requests

Tokens refill at rate: 1000 per minute
```

### **Caching Strategy**
```
Request for Apple stock price:
1st request:  Check Redis cache → MISS → Query database → Store in cache
2nd request:  Check Redis cache → HIT → Return instantly

This prevents unnecessary database queries
```

### **Circuit Breaker Pattern**
```
If a service is having problems:

State: CLOSED (Normal)
       ↓ (5 consecutive failures)
State: OPEN (Fail-fast for 30 seconds)
       ↓ (30 seconds pass)
State: HALF-OPEN (Test one request)
       ↓ (succeeds)
State: CLOSED (Resume normal operation)
```

---

## 📈 Monitoring & Observability

### **What is monitored?**

1. **Application Insights**
   - Exception tracking
   - Performance metrics
   - Distributed tracing across services

2. **Correlation IDs**
   ```
   Request enters API → Assigned unique ID: abc123
   Flows through Order Service → Uses same ID: abc123
   Flows through Portfolio Service → Uses same ID: abc123
   
   Can trace entire request path using abc123 in logs
   ```

3. **Structured Logging (Serilog)**
   ```json
   {
     "timestamp": "2024-03-16T10:30:00Z",
     "level": "INFO",
     "message": "Order created",
     "userId": 101,
     "orderId": 12345,
     "correlationId": "abc123",
     "service": "OrderProcessing"
   }
   ```

4. **Business Metrics**
   - Orders per second
   - Average order processing time
   - Total portfolio value managed
   - P&L across all users

5. **Health Checks**
   ```
   Kubernetes periodically checks:
   - Is MarketData.API responding?
   - Is database connection active?
   - Is Service Bus accessible?
   
   If unhealthy → restart the service
   ```

---

## 🚀 Deployment Pipeline (CI/CD)

### **How code gets to production:**

```
Step 1: Developer commits code to GitHub
              ↓
Step 2: GitHub Actions triggered
              ↓
Step 3: Run automated tests
              ↓ If tests pass
Step 4: Build Docker containers for each service
              ↓
Step 5: Push containers to Azure Container Registry
              ↓
Step 6: Deploy to Kubernetes cluster
              ↓
Step 7: Run smoke tests in production
              ↓
Step 8: Send Slack notification "Deployment successful"
```

---

## 💡 Key Takeaways for Freshers

1. **Microservices** = Breaking big app into small independent services
2. **CQRS** = Separate read and write operations for performance
3. **Event-Driven** = Services communicate through events, not direct calls
4. **Saga Pattern** = Handle distributed transactions with compensating transactions
5. **Eventual Consistency** = Accept that system is temporarily inconsistent but eventually consistent
6. **Circuit Breaker** = Prevent cascade failures by failing fast
7. **Azure Services** = Cloud-native approach (no server management)
8. **Monitoring** = Essential for distributed systems (trace requests across services)

---

## 📚 Technologies Used

| Component | Technology | Why? |
|-----------|-----------|------|
| API Gateway | Azure API Management | Route, authenticate, rate limit |
| Services | C# / .NET | Enterprise language, high performance |
| Databases | SQL Server + Cosmos DB | ACID + Real-time NoSQL |
| Message Bus | Azure Service Bus | Reliable event delivery |
| Serverless | Azure Functions | Durable Functions for workflows |
| Containers | Docker | Package services for deployment |
| Orchestration | Kubernetes | Manage containers at scale |
| Monitoring | Application Insights | Distributed tracing |
| CI/CD | GitHub Actions | Automated deployment |
| Caching | Redis | Fast data access |
| Auth | Azure AD / Entra ID | Enterprise authentication |

---

## 🎓 Interview Questions Practice

This document includes common interview questions about this architecture. Review them to understand:
- Why each technology choice was made
- How different patterns solve problems
- Trade-offs between different approaches

Good luck! 🚀
