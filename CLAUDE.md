# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BookPricesWatcher is a full-stack web application for scraping and comparing book prices across online retailers. It uses Clean Architecture with ASP.NET Core 8.0 backend, Angular 20 frontend, and PostgreSQL database.

## Common Commands

### Backend (.NET)

```bash
# Build solution
dotnet build

# Run API (available at http://localhost:5177)
dotnet run --project API/SherlockAPI.csproj

# Run with hot reload
dotnet watch --project API/SherlockAPI.csproj

# Database migrations
dotnet ef database update --project BookPricesWatcher.Data/Sherlock.Data.csproj --startup-project API/SherlockAPI.csproj

# Add new migration
dotnet ef migrations add MigrationName --project BookPricesWatcher.Data/Sherlock.Data.csproj --startup-project API/SherlockAPI.csproj
```

### Frontend (Angular)

```bash
cd Client

# Install dependencies
npm install

# Development server (http://localhost:4200)
npm start

# Production build
npm run build

# Run tests
npm test
```

### Tests (.NET)

```bash
# Run all tests
dotnet test BookPricesWatcher.Tests/Sherlock.Tests.csproj

# Run with verbose output
dotnet test BookPricesWatcher.Tests/Sherlock.Tests.csproj --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthControllerTests"
```

Test project: `BookPricesWatcher.Tests/` using xUnit, FluentAssertions, Moq

## Architecture

The solution follows Clean Architecture with these layers:

- **BookPricesWatcher.Domain/** - Entities (User, Book, BookPrice, Provider, Query, Transaction, Scraper, Token) and repository interfaces
- **BookPricesWatcher.Business/** - Business logic, services, scrapers (W16Engine), DTOs
- **BookPricesWatcher.Data/** - EF Core DbContext (SherlockDbContext), repositories, migrations
- **BookPricesWatcher.Infrastructure/** - Cross-cutting concerns (cache, resilience)
- **API/** - ASP.NET Core Web API controllers, JWT authentication (TokenService), DI configuration
- **Client/** - Angular 20 app with standalone components pattern

### Key Patterns

- Repository pattern for data access (IUserRepository → UserRepository)
- DI registration via extension methods on IServiceCollection in each layer
- JWT Bearer authentication configured in `API/Configurations/Configurator.cs`
- Angular auth service manages tokens in localStorage with @auth0/angular-jwt
- Snake_case naming convention for database tables and columns (EF Core UseSnakeCaseNamingConvention)

### Database

PostgreSQL connection configured in `API/appsettings.json`. DbContext: `BookPricesWatcher.Data/Context/SherlockDbContext.cs`

#### Key Tables

- **transactions** - Aggregates search transactions (user, cost, timing, result type)
- **queries** - Individual provider queries (provider_id, price, title, response time)
- **providers** - Book retailers (93 providers configured in Provider.cs)
- **books** - Book catalog
- **book_prices** - Historical price data
- **users** - User accounts

#### Transaction/Query Model

Each search creates one `Transaction` with multiple `Query` records:
- `Transaction`: Who searched, total cost, execution time, best result reference
- `Query`: Individual provider result (provider_id, transaction_id, price, title, success/error)

### Web Scraping

Core engine in `BookPricesWatcher.Business/Core/Base/W16Engine.cs`:
- Configurable parallelism via `MaxDegreeOfParallelism` (default: 10)
- Detailed metrics logging (response times, P50/P95, throughput)
- Uses `ScraperFactory` to create scrapers by provider category

Scrapers in `BookPricesWatcher.Business/Core/Scrapers/`:
- **CedetSingleSearchHttpClient** - Primary scraper using HttpClient + HtmlAgilityPack
  - Multiple CSS/XPath selectors for robust HTML parsing
  - Polly retry policy (2 retries, exponential backoff)
  - Brazilian price format support (R$ 1.234,56)

**Scraper Selection:** O scraper é definido pelo `ProviderCategoryEnum` do provider. Os 93 providers configurados são do tipo `Cedet` (lojas WooCommerce com estrutura HTML similar), por isso todos usam o `CedetSingleSearchHttpClient`. Para adicionar novos tipos de sites (ex: Amazon, MercadoLivre), basta criar um novo scraper e associá-lo à categoria correspondente no `ScraperFactory`.

### Caching & Resilience

- Redis cache (optional, falls back to in-memory)
- Polly for retry policies and circuit breaker
- `ResilientScraperWrapper` for scraper resilience

## API Endpoints

- Swagger UI: `http://localhost:5177/swagger` (development only)
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `GET /api/BookSearch?title={title}` - Search book prices
- `POST /api/BookSearch` - Search with body (title, isbn, author, providerUrls)
- `GET /api/Providers` - List all providers
- `GET /api/Providers/active` - List active providers only
- `POST /api/Cart/optimize` - Optimize cart for best prices

## Key Entry Points

- Backend: `API/Program.cs`
- Frontend: `Client/src/app/app.ts` with routes in `app.routes.ts`
- Auth flow: `API/Controllers/AuthController.cs` ↔ `Client/src/app/services/auth-service.ts`
- Search flow: `BookSearchController` → `W16Engine.ExecuteTransaction()` → `Scrapers`

## Providers

93 book retailers configured in `BookPricesWatcher.Domain/Entities/Provider.cs`:
- Each has: Id, Name, Url, ProviderCategoryEnum, MinFreeShipping, BaseShippingCost, IsActive
- Categories: Cedet (WooCommerce stores), Amazon, MercadoLivre, etc.
- Use `Provider.AllSources` to access the static list
