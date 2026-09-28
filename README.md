# Sales Analytics & Performance Platform

Welcome to the Sales Analytics & Performance Platform. This project is a rigorous demonstration of **Clean Architecture**, **Domain-Driven Design (DDD)**, and **Entity Framework Core** optimizations designed for high-performance enterprise applications. 

## 🚀 Как запустить проект (Локальный запуск)

Проект полностью контейнеризирован и запускается одной командой согласно требованиям ТЗ.
Откройте терминал в корневой папке проекта и выполните:

```bash
docker compose up --build
```

После старта сервисов и применения миграций БД автоматически наполнится seed-данными. 
Сервисы будут доступны по следующим адресам:
*   **Dashboard (Frontend):** http://localhost:3000
*   **API (Backend):** http://localhost:8081
*   **PostgreSQL:** localhost:5433 (проброшен на нестандартный порт для избежания конфликтов с локальными БД)

## 🏛️ Architecture & Philosophy
This backend was built strictly adhering to Clean Architecture principles. The application is decoupled into independent layers, ensuring that the domain model remains pure and untethered to external dependencies or infrastructure concerns. 

- **Domain Layer:** Contains pristine C# Entities (Sale, Product, Manager). Collections are encapsulated via `IReadOnlyCollection` and `HashSet<T>` to guarantee O(1) traversal and structural immutability. Anemic domain models have been eliminated.
- **Application Layer:** Handles use cases and orchestrates domain models using MediatR and isolated Services (AnalyticsService, SalesService).
- **Infrastructure Layer:** Implements Data Access via EF Core with raw SQL optimization fallbacks for aggregations.
- **API Layer:** Minimal controllers exposing RESTful endpoints, completely decoupled from the underlying database schema.

## 📊 Business Rules (Легенда)
- **Revenue**: Calculated as `SalePrice * Quantity`. Excludes Refunded and Cancelled orders.
- **Gross Profit**: Calculated as `(SalePrice - CostPrice) * Quantity`. Excludes Refunded and Cancelled orders.
- **Statuses**: Refunded and Cancelled are completely excluded from Revenue and Profit calculations, but remain in the database for historical record.
- **Previous Period Comparison**: Metrics are compared against the exact same duration immediately preceding the selected date range. For example, if "Last 30 Days" is selected, the previous period is the 30 days before that.

## 🚀 Key Technical Highlights
- **No-Tracking Queries:** Read-only analytics endpoints aggressively utilize `.AsNoTracking()` to bypass EF Core's Change Tracker overhead, significantly boosting throughput.
- **Query Vectorization & Indexing:** Added specialized partial indexes in PostgreSQL (e.g., `HasFilter("\"Status\" = 'Paid'")`) to optimize our most frequent analytical query patterns.
- **Optimistic Concurrency:** Applied `[Timestamp]` (`uint Version`) mapping to PostgreSQL's native `xmin` system column to flawlessly handle concurrency during high-throughput parallel data mutations without expensive locks.
- **Stateless Seeding:** Implemented a robust data seeder using deterministic randomized distributions to create realistic analytical test data within strict transaction boundaries.

## ⏳ Что не успели за 8 часов
- Интеграционные тесты (E2E) с поднятием реальной БД через Testcontainers. Написаны только Unit-тесты.
- Логирование действий пользователя (Audit Trails) и расширенный мониторинг через OpenTelemetry/Prometheus.
- Продвинутая фильтрация (например, выбор конкретных категорий товаров на дашборде).
- Пагинация для таблицы последних продаж (выводится только топ-50 последних записей для производительности).

## 💡 Что улучшили бы дальше в production
- **Кеширование**: Внедрение Redis для кеширования тяжелых аналитических запросов с инвалидацией при добавлении новых продаж.
- **Асинхронные очереди**: Обработка поступающих продаж через RabbitMQ/Kafka для сглаживания нагрузки (load leveling).
- **CQRS**: Полное разделение моделей на запись (Write Model) и чтение (Read Model - Materialized Views в PostgreSQL).
- **CI/CD Pipeline**: Настроить GitHub Actions для автоматической сборки, тестирования и деплоя.
- **Авторизация**: Добавление JWT/OAuth2 для защиты API.

---

## 👨‍💻 Автор
**Николай Фокин**
* Senior Software Engineer & System Architect (15+ лет коммерческой разработки)
* Кандидат наук (ПМИС, ОГУ им. И.С. Тургенева, 2011)
* **Telegram:** **@nikolay_fokin**
* **Локация:** Орел, РФ