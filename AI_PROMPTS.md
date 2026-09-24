# Журнал запросов к AI (AI_PROMPTS.md)

## 09:30 — Claude 4.5 Sonnet (через Cursor)
Я создал начальную структуру проекта и добавил папку `.cursor/rules/` с гайдлайнами для архитектуры React и ASP.NET Core. 
Пожалуйста, изучи требования в `TASK.md`. 
Сгенерируй `docker-compose.yml` для поднятия PostgreSQL, пустого ASP.NET Core бэкенда и Vite React фронтенда. Все должно подниматься одной командой.

## 09:45 — Claude 4.5 Sonnet (через Cursor)
Проинициализируй проект ASP.NET Core 8 Web API в папке `/backend`.
Строго следуй правилам из `.cursor/rules/dotnet-architecture.md` (никаких N+1, разделение ответственности).
Создай EF Core DbContext и сущности домена: Manager, Customer, Category, Product, Sale, SaleItem. Настрой связи между ними.
