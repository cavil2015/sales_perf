# Sales Analytics & Performance Platform

Welcome to the Sales Analytics & Performance Platform. This project is a rigorous demonstration of **Clean Architecture**, **Domain-Driven Design (DDD)**, and **Entity Framework Core** optimizations designed for high-performance enterprise applications. 

## 🚀 Как запустить проект (локальный запуск)

Проект полностью контейнеризован и запускается одной командой.
Проверьте, что у вас свободен порт 5433 для PostgreSQL.

`ash
docker compose up --build
`

После старта бэкенд и база данных автоматически накатят миграции и заполнятся seed-данными. 
Проект будет доступен по следующим адресам:
*   **Dashboard (Frontend):** http://localhost:3000
*   **API (Backend):** http://localhost:8081
*   **PostgreSQL:** localhost:5433 (Проброшен на хост-машину, чтобы вы могли подключиться из DBeaver или pgAdmin)

## 🏛️ Architecture & Philosophy
This backend was built strictly adhering to Clean Architecture principles. The application is decoupled into independent layers, ensuring that the domain model remains pure and untethered to external dependencies or infrastructure concerns. 

- **Domain Layer:** Contains pristine C# Entities (Sale, Product, Manager). Collections are encapsulated via IReadOnlyCollection and HashSet<T> to guarantee O(1) traversal and structural immutability. Anemic domain models have been eliminated.
- **Application Layer:** Handles use cases and orchestrates domain models using MediatR and isolated Services (AnalyticsService, SalesService).
- **Infrastructure Layer:** Implements Data Access via EF Core with raw SQL optimization fallbacks for aggregations.
- **API Layer:** Minimal controllers exposing RESTful endpoints, completely decoupled from the underlying database schema.

## 💼 Business Rules (Бизнес-требования)
- **Revenue (Выручка)**: Рассчитывается как SalePrice * Quantity. Исключает заказы со статусами Refunded и Cancelled.
- **Gross Profit (Маржинальная прибыль)**: Рассчитывается как (SalePrice - CostPrice) * Quantity. Исключает заказы со статусами Refunded и Cancelled.
- **Statuses (Статусы)**: Отмененные сделки полностью исключаются из аналитики, но остаются в базе данных для истории.
- **Previous Period Comparison (Динамика)**: Метрики сравниваются с точно таким же по длительности периодом, непосредственно предшествующим выбранному диапазону дат (например, если выбрано "Последние 30 дней", сравнение идет с 30 днями до этого).

## ⚡ Key Technical Highlights
- **No-Tracking Queries:** Аналитические эндпоинты агрессивно используют .AsNoTracking() для обхода оверхеда Change Tracker в EF Core, значительно повышая пропускную способность.
- **Query Vectorization & Indexing:** Добавлены специализированные частичные индексы в PostgreSQL (например, HasFilter("\"Status\" = 'Paid'")) для оптимизации самых частых аналитических запросов.
- **Optimistic Concurrency:** Применен маппинг [Timestamp] (uint Version) на системную колонку xmin в PostgreSQL для безупречной обработки конкурентных мутаций без блокировок.
- **Stateless Seeding:** Написан мощный генератор данных с использованием детерминированного случайного распределения Гаусса (через Box-Muller transform) для создания реалистичных аналитических данных.

## ⌛ Что мы НЕ успели за 8 часов
- Интеграционные тесты (E2E) с поднятием реальной БД через Testcontainers. Написаны только Unit-тесты.
- Полноценный аудит изменений (Audit Trails) с метриками для OpenTelemetry/Prometheus.
- Кэширование аналитики (например, Redis).

## 🌍 Что можно улучшить для production
- **Кэширование**: Внедрить Redis для кэширования агрегированных графиков и минимизации нагрузки на базу данных.
- **Очереди сообщений**: Настроить асинхронную обработку через RabbitMQ/Kafka для создания заказов (load leveling).
- **CQRS**: Полностью разделить таблицы на запись (Write Model) и чтение (Read Model - Materialized Views в PostgreSQL).
- **CI/CD Pipeline**: Настроить GitHub Actions для автоматической сборки, тестирования и деплоя.
- **Безопасность**: Добавление JWT/OAuth2 для защиты API.

---

## Контакты автора
**Николай Фокин**
* Senior Software Engineer & System Architect (15+ лет коммерческой разработки)
* Кандидат наук (МГУ, им М.В. Ломоносова, 2011)
* **Telegram:** **@nikolay_fokin**
