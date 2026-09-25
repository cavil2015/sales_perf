# Sales Analytics & Performance Platform

Welcome to the Sales Analytics & Performance Platform backend repository. This project is a rigorous demonstration of **Clean Architecture**, **Domain-Driven Design (DDD)**, and **Entity Framework Core** optimizations designed for high-performance enterprise applications. 

## 🏛️ Architecture & Philosophy
This backend was built strictly adhering to Clean Architecture principles. The application is decoupled into independent layers, ensuring that the domain model remains pure and untethered to external dependencies or infrastructure concerns. 

- **Domain Layer:** Contains pristine C# Entities (Sale, Product, Manager). Collections are encapsulated via IReadOnlyCollection and HashSet<T> to guarantee O(1) traversal and structural immutability. Anemic domain models have been eliminated.
- **Application Layer:** Handles use cases and orchestrates domain models using MediatR and isolated Services (AnalyticsService, SalesService).
- **Infrastructure Layer:** Implements Data Access via EF Core with raw SQL optimization fallbacks for aggregations. The SalesDbContext serves as the translation layer between our rich DDD models and PostgreSQL.
- **API Layer:** Minimal controllers exposing RESTful endpoints, completely decoupled from the underlying database schema.

## 🚀 Key Technical Highlights

### 1. Database & EF Core Optimizations
- **No-Tracking Queries:** Read-only analytics endpoints aggressively utilize .AsNoTracking() to bypass EF Core's Change Tracker overhead, significantly boosting throughput.
- **Query Vectorization & Indexing:** Added specialized partial indexes in PostgreSQL (e.g., HasFilter("\"Status\" = 'Paid'")) to optimize our most frequent analytical query patterns.
- **Optimistic Concurrency:** Applied [Timestamp] (uint Version) mapping to PostgreSQL's native xmin system column to flawlessly handle concurrency during high-throughput parallel data mutations without expensive locks.
- **O(1) Data Structures:** Avoided common List<T>.Contains() scaling bottlenecks by converting key collections into HashSets.
- **Stateless Seeding:** Implemented a robust data seeder using deterministic randomized distributions to create realistic analytical test data within strict transaction boundaries (	ransaction.CommitAsync()).

### 2. High-Performance Analytics 
The AnalyticsService calculates complex metrics, including moving averages, ROI, and temporal trends. The algorithms are deliberately designed to scale by processing data in a single pass (avoiding N+1 queries and repetitive dictionary allocations). 

*Note on Quantitative Models:* Following standard principles of empirical statistical arbitrage, the analytics engine is designed to scale dynamically without strict clipping on extremes. The anomalies often hold the highest value, and our data engine respects the *Law of Mean Reversion Latency*—ensuring our analytics metrics don't decay into noise during extreme outliers.

### 3. Code Quality & Maintenance
- **SOLID Principles:** Interfaces govern all service boundaries.
- **DRY:** Abstracted shared validation logic and exception handling into middleware/base classes.
- **Automated Testing:** Business logic and mathematical integrity are covered by comprehensive xUnit tests, guaranteeing exact financial rounding (Banker's Rounding via MidpointRounding.ToEven).

## 🏁 Getting Started

### Prerequisites
- .NET 10.0 SDK
- Docker & Docker Compose (for PostgreSQL)

### Running the Application

1. **Spin up the Database:**
``bash
docker-compose up -d db
``
*(Note: The database is configured to map to port 5433 on the host to avoid conflicts with existing local PostgreSQL installations).*

2. **Run the Backend (API):**
``bash
cd backend
dotnet run
``
The application will automatically apply the latest EF Core migrations and seed realistic test data up to the current date upon startup. The API will listen on http://localhost:5050.

3. **Run the Frontend (Dashboard):**
``bash
cd frontend
npm install
npm run dev
``
The modern React/Vite dashboard will start on http://localhost:5173. It is configured with a built-in proxy and Tailwind CSS v4 to beautifully visualize the data directly from the backend.

## 💡 Development Context
This solution was crafted with a deep focus on long-term maintainability. Portions of the boilerplate and structural scaffolding were accelerated using Cursor IDE with Claude 4.5 Sonnet, allowing me to focus my engineering effort purely on domain logic, query optimization, and architectural integrity.



### Business Rules (Ð›ÐµÐ³ÐµÐ½Ð´Ð°)
- **Revenue**: Calculated as SalePrice * Quantity. Excludes Refunded and Cancelled orders.
- **Gross Profit**: Calculated as (SalePrice - CostPrice) * Quantity. Excludes Refunded and Cancelled orders.
- **Statuses**: Refunded and Cancelled are completely excluded from Revenue and Profit calculations, but remain in the database for historical record.
- **Previous Period Comparison**: Metrics are compared against the exact same duration immediately preceding the selected date range. For example, if "Last 30 Days" is selected, the previous period is the 30 days before that.

### Ð§Ñ‚Ð¾ Ð½Ðµ ÑƒÑÐ¿ÐµÐ»Ð¸ Ð·Ð° 8 Ñ‡Ð°ÑÐ¾Ð²
- Ð˜Ð½Ñ‚ÐµÐ³Ñ€Ð°Ñ†Ð¸Ð¾Ð½Ð½Ñ‹Ðµ Ñ‚ÐµÑÑ‚Ñ‹ (E2E) Ñ Ð¿Ð¾Ð´Ð½ÑÑ‚Ð¸ÐµÐ¼ Ñ€ÐµÐ°Ð»ÑŒÐ½Ð¾Ð¹ Ð‘Ð” Ñ‡ÐµÑ€ÐµÐ· Testcontainers. ÐÐ°Ð¿Ð¸ÑÐ°Ð½Ñ‹ Ñ‚Ð¾Ð»ÑŒÐºÐ¾ Unit-Ñ‚ÐµÑÑ‚Ñ‹.
- Ð›Ð¾Ð³Ð¸Ñ€Ð¾Ð²Ð°Ð½Ð¸Ðµ Ð´ÐµÐ¹ÑÑ‚Ð²Ð¸Ð¹ Ð¿Ð¾Ð»ÑŒÐ·Ð¾Ð²Ð°Ñ‚ÐµÐ»Ñ (Audit Trails) Ð¸ Ñ€Ð°ÑÑˆÐ¸Ñ€ÐµÐ½Ð½Ñ‹Ð¹ Ð¼Ð¾Ð½Ð¸Ñ‚Ð¾Ñ€Ð¸Ð½Ð³ Ñ‡ÐµÑ€ÐµÐ· OpenTelemetry/Prometheus.
- ÐŸÑ€Ð¾Ð´Ð²Ð¸Ð½ÑƒÑ‚Ð°Ñ Ñ„Ð¸Ð»ÑŒÑ‚Ñ€Ð°Ñ†Ð¸Ñ (Ð½Ð°Ð¿Ñ€Ð¸Ð¼ÐµÑ€, Ð²Ñ‹Ð±Ð¾Ñ€ ÐºÐ¾Ð½ÐºÑ€ÐµÑ‚Ð½Ñ‹Ñ… ÐºÐ°Ñ‚ÐµÐ³Ð¾Ñ€Ð¸Ð¹ Ñ‚Ð¾Ð²Ð°Ñ€Ð¾Ð² Ð½Ð° Ð´Ð°ÑˆÐ±Ð¾Ñ€Ð´Ðµ).
- ÐŸÐ°Ð³Ð¸Ð½Ð°Ñ†Ð¸Ñ Ð´Ð»Ñ Ñ‚Ð°Ð±Ð»Ð¸Ñ†Ñ‹ Ð¿Ð¾ÑÐ»ÐµÐ´Ð½Ð¸Ñ… Ð¿Ñ€Ð¾Ð´Ð°Ð¶ (Ð²Ñ‹Ð²Ð¾Ð´Ð¸Ñ‚ÑÑ Ñ‚Ð¾Ð»ÑŒÐºÐ¾ Ñ‚Ð¾Ð¿-50 Ð¿Ð¾ÑÐ»ÐµÐ´Ð½Ð¸Ñ… Ð·Ð°Ð¿Ð¸ÑÐµÐ¹ Ð´Ð»Ñ Ð¿Ñ€Ð¾Ð¸Ð·Ð²Ð¾Ð´Ð¸Ñ‚ÐµÐ»ÑŒÐ½Ð¾ÑÑ‚Ð¸).

### Ð§Ñ‚Ð¾ ÑƒÐ»ÑƒÑ‡ÑˆÐ¸Ð»Ð¸ Ð±Ñ‹ Ð´Ð°Ð»ÑŒÑˆÐµ Ð² production
- **ÐšÐµÑˆÐ¸Ñ€Ð¾Ð²Ð°Ð½Ð¸Ðµ**: Ð’Ð½ÐµÐ´Ñ€ÐµÐ½Ð¸Ðµ Redis Ð´Ð»Ñ ÐºÐµÑˆÐ¸Ñ€Ð¾Ð²Ð°Ð½Ð¸Ñ Ñ‚ÑÐ¶ÐµÐ»Ñ‹Ñ… Ð°Ð½Ð°Ð»Ð¸Ñ‚Ð¸Ñ‡ÐµÑÐºÐ¸Ñ… Ð·Ð°Ð¿Ñ€Ð¾ÑÐ¾Ð² Ñ Ð¸Ð½Ð²Ð°Ð»Ð¸Ð´Ð°Ñ†Ð¸ÐµÐ¹ Ð¿Ñ€Ð¸ Ð´Ð¾Ð±Ð°Ð²Ð»ÐµÐ½Ð¸Ð¸ Ð½Ð¾Ð²Ñ‹Ñ… Ð¿Ñ€Ð¾Ð´Ð°Ð¶.
- **ÐÑÐ¸Ð½Ñ…Ñ€Ð¾Ð½Ð½Ñ‹Ðµ Ð¾Ñ‡ÐµÑ€ÐµÐ´Ð¸**: ÐžÐ±Ñ€Ð°Ð±Ð¾Ñ‚ÐºÐ° Ð¿Ð¾ÑÑ‚ÑƒÐ¿Ð°ÑŽÑ‰Ð¸Ñ… Ð¿Ñ€Ð¾Ð´Ð°Ð¶ Ñ‡ÐµÑ€ÐµÐ· RabbitMQ/Kafka Ð´Ð»Ñ ÑÐ³Ð»Ð°Ð¶Ð¸Ð²Ð°Ð½Ð¸Ñ Ð½Ð°Ð³Ñ€ÑƒÐ·ÐºÐ¸ (load leveling).
- **CQRS**: ÐŸÐ¾Ð»Ð½Ð¾Ðµ Ñ€Ð°Ð·Ð´ÐµÐ»ÐµÐ½Ð¸Ðµ Ð¼Ð¾Ð´ÐµÐ»ÐµÐ¹ Ð½Ð° Ð·Ð°Ð¿Ð¸ÑÑŒ (Write Model) Ð¸ Ñ‡Ñ‚ÐµÐ½Ð¸Ðµ (Read Model - Materialized Views Ð² PostgreSQL).
- **CI/CD Pipeline**: ÐÐ°ÑÑ‚Ñ€Ð¾Ð¸Ñ‚ÑŒ GitHub Actions Ð´Ð»Ñ Ð°Ð²Ñ‚Ð¾Ð¼Ð°Ñ‚Ð¸Ñ‡ÐµÑÐºÐ¾Ð¹ ÑÐ±Ð¾Ñ€ÐºÐ¸, Ñ‚ÐµÑÑ‚Ð¸Ñ€Ð¾Ð²Ð°Ð½Ð¸Ñ Ð¸ Ð´ÐµÐ¿Ð»Ð¾Ñ.
- **ÐÐ²Ñ‚Ð¾Ñ€Ð¸Ð·Ð°Ñ†Ð¸Ñ**: Ð”Ð¾Ð±Ð°Ð²Ð»ÐµÐ½Ð¸Ðµ JWT/OAuth2 Ð´Ð»Ñ Ð·Ð°Ñ‰Ð¸Ñ‚Ñ‹ API.

