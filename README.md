# Sales Analytics & Performance Dashboard

Welcome to the **Sales Analytics & Performance Platform**. This project is a rigorous demonstration of **Clean Architecture**, **Domain-Driven Design (DDD)**, and **Entity Framework Core** optimizations designed for high-performance enterprise B2B SaaS applications.

## 🚀 Quick Start (Docker)

The application is completely containerized. You only need Docker installed.

```bash
docker compose up --build
```

The database will automatically run EF Core migrations and securely seed realistic data.
The services will be available at:
*   **Dashboard (Frontend):** http://localhost:3000
*   **API (Backend):** http://localhost:8081
*   **PostgreSQL:** localhost:5433 (Exposed to host for easy inspection via DBeaver/pgAdmin)

## 🏗 Architecture & Philosophy

This backend was built strictly adhering to Clean Architecture principles. The application is decoupled into independent layers, ensuring that the domain model remains pure and untethered to external dependencies or infrastructure concerns. 

- **Domain Layer:** Contains pristine C# Entities (Sale, Product, Manager). Collections are encapsulated via `IReadOnlyCollection` and `HashSet<T>` to guarantee O(1) traversal and structural immutability. Anemic domain models have been eliminated.
- **Application Layer:** Handles use cases and orchestrates domain models using MediatR and isolated Services.
- **Infrastructure Layer:** Implements Data Access via EF Core with raw SQL optimization fallbacks for aggregations.
- **API Layer:** Minimal controllers exposing RESTful endpoints, completely decoupled from the underlying database schema.

## 📈 Business Rules (Metrics Logic)
- **Revenue**: Calculated as `SalePrice * Quantity`. Excludes sales with `Refunded` or `Cancelled` statuses.
- **Gross Profit**: Calculated as `(SalePrice - CostPrice) * Quantity`. Excludes `Refunded` and `Cancelled` statuses.
- **Statuses**: Refunded sales are fully excluded from analytics, rather than rendering as negative items.
- **Previous Period Comparison**: Calculated dynamically over a mirrored date range in the past (e.g. if looking at the "Last 30 Days", it compares against the 30 days strictly prior to that).

## 🛠 Key Technical Highlights
- **No-Tracking Queries:** Read operations are optimized using `.AsNoTracking()` to bypass the EF Core Change Tracker, drastically reducing memory footprint.
- **Query Vectorization & Indexing:** Partial indexes (e.g. `HasFilter("\"Status\" = 'Paid'")`) are applied on the DB level to speed up the heaviest aggregate queries.
- **Optimistic Concurrency:** Row-level versioning uses `[Timestamp]` (`uint Version`) mapped to PostgreSQL's native `xmin` column for lock-free concurrency.
- **Stateless Seeding:** Uses realistic Gaussian distribution (Box-Muller transform) to generate plausible analytical data without external files.

## 🔮 Roadmap / Future Enhancements
- **Caching**: Implement Redis for distributed caching of heavy aggregations.
- **Message Queues**: Integrate RabbitMQ/Kafka for load leveling and asynchronous processing.
- **CQRS**: Fully isolate the Write Model from the Read Model (using PostgreSQL Materialized Views).
- **CI/CD Pipeline**: Automated GitHub Actions for linting, testing, and Docker push.

---

## 📬 Contacts
**Николай Фокин (Nikolay Fokin)**
* Senior Software Engineer & System Architect
* **Telegram:** [@nikolay_fokin](https://t.me/nikolay_fokin)
