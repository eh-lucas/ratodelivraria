# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Sherlock is a full-stack web application for scraping and comparing book prices across online retailers. It uses Clean Architecture with ASP.NET Core 8.0 backend, Angular 20 frontend, and PostgreSQL database.

## Common Commands

### Backend (.NET)

```bash
# Build solution
dotnet build

# Run API (available at http://localhost:5177)
dotnet run --project Sherlock.Api/Sherlock.Api.csproj

# Run with hot reload
dotnet watch --project Sherlock.Api/Sherlock.Api.csproj

# Database migrations
dotnet ef database update --project Sherlock.Data/Sherlock.Data.csproj --startup-project Sherlock.Api/Sherlock.Api.csproj

# Add new migration
dotnet ef migrations add MigrationName --project Sherlock.Data/Sherlock.Data.csproj --startup-project Sherlock.Api/Sherlock.Api.csproj
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
dotnet test Sherlock.Tests/Sherlock.Tests.csproj

# Run with verbose output
dotnet test Sherlock.Tests/Sherlock.Tests.csproj --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthControllerTests"
```

Test project: `Sherlock.Tests/` using xUnit, FluentAssertions, Moq

### Docker

```bash
# Start all containers (PostgreSQL, Redis, API, Client)
docker-compose up -d

# Rebuild and start specific container
docker-compose build client && docker-compose up -d

# View logs
docker-compose logs -f api
docker-compose logs --tail=50 client

# Stop all containers
docker-compose down

# Execute SQL in PostgreSQL container
docker exec sherlock-postgres psql -U sherlock_admin -d sherlock_dev_db -c "SELECT * FROM users;"
```

**Container names:** `sherlock-postgres`, `sherlock-redis`, `sherlock-api`, `sherlock-client`

**URLs when running with Docker:**
- Frontend: http://localhost:4200
- API: http://localhost:5177
- Swagger: http://localhost:5177/swagger

## Architecture

The solution follows Clean Architecture with these layers:

- **Sherlock.Domain/** - Entities (User, Book, BookPrice, Provider, Query, Transaction, Scraper, Token) and repository interfaces
- **Sherlock.Business/** - Business logic, services, scrapers (W16Engine), DTOs
- **Sherlock.Data/** - EF Core DbContext (SherlockDbContext), repositories, migrations
- **Sherlock.Infrastructure/** - Cross-cutting concerns (cache, resilience)
- **Sherlock.Api/** - ASP.NET Core Web API controllers, JWT authentication (TokenService), DI configuration
- **Client/** - Angular 20 app with standalone components pattern

### Key Patterns

- Repository pattern for data access (IUserRepository → UserRepository)
- DI registration via extension methods on IServiceCollection in each layer
- JWT Bearer authentication configured in `Sherlock.Api/Configurations/Configurator.cs`
- Angular auth service manages tokens in localStorage with @auth0/angular-jwt
- Snake_case naming convention for database tables and columns (EF Core UseSnakeCaseNamingConvention)
- **DbContextFactory** for concurrent database operations (required for parallel searches)

### DbContextFactory Pattern

Para operações paralelas de banco de dados (ex: buscar preços em múltiplos providers simultaneamente), usamos `IDbContextFactory<SherlockDbContext>` ao invés do DbContext injetado diretamente.

**Por quê?** O DbContext não é thread-safe. Quando `Task.WhenAll` executa múltiplas queries em paralelo, cada task precisa de sua própria instância do DbContext.

**Configuração em `ServiceCollectionExtensions.cs`:**
```csharp
services.AddDbContextFactory<SherlockDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
```

**Uso nos repositórios:**
```csharp
public class QueryRepository : BaseRepository<Query>, IQueryRepository
{
    private readonly IDbContextFactory<SherlockDbContext> _contextFactory;

    public async Task<Query?> GetCachedQueryAsync(string isbn, int providerId, int cacheTimeMinutes)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Query>()...
    }
}
```

**Repositórios que usam DbContextFactory:** `QueryRepository`, `TransactionRepository`

### Database

PostgreSQL connection configured in `Sherlock.Api/appsettings.json`. DbContext: `Sherlock.Data/Context/SherlockDbContext.cs`

**Auto-Migration:** O `Program.cs` aplica migrations automaticamente no startup:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SherlockDbContext>();
    db.Database.Migrate();
}
```

#### Key Tables

- **transactions** - Aggregates search transactions (user, cost, timing, result type)
- **queries** - Individual provider queries (provider_id, price, title, response time)
- **providers** - Book retailers (93 providers configured in Provider.cs)
- **books** - Book catalog
- **book_prices** - Historical price data
- **users** - User accounts (com available_credits, total_credits_used)
- **credit_packages** - Pacotes de créditos disponíveis para compra
- **credit_transactions** - Histórico de compras/uso de créditos

#### Transaction/Query Model

Each search creates one `Transaction` with multiple `Query` records:
- `Transaction`: Who searched, total cost, execution time, best result reference
- `Query`: Individual provider result (provider_id, transaction_id, price, title, success/error)

#### Persistência Automática

O `W16Engine` persiste automaticamente cada transação via `TransactionPersistenceService`:
1. Cria `Transaction` com `InputParameters` (JSON com título, ISBN, autor)
2. Salva todas as `Query` entities em batch
3. Identifica a melhor query (menor preço) e atualiza `BestQueryId`
4. Serializa erros em `Transaction.Errors` (JSON)

**Fluxo:**
```
Controller (autenticado) → Service → W16Engine.ExecuteTransaction(requestor, userId)
                                         ↓
                               TransactionPersistenceService.PersistAsync()
```

**Importante:** `userId` é obrigatório - todos os endpoints de busca requerem autenticação.

### Web Scraping

Core engine in `Sherlock.Business/Core/Base/W16Engine.cs`:
- Configurable parallelism via `MaxDegreeOfParallelism` (default: 10)
- Detailed metrics logging (response times, P50/P95, throughput)
- Uses `ScraperFactory` to create scrapers by provider category
- **Persistência automática**: Após cada busca, persiste Transaction e Queries no banco via `ITransactionPersistenceService`

Scrapers in `Sherlock.Business/Core/Scrapers/`:
- **CedetSingleSearchHttpClient** - Primary scraper using HttpClient + HtmlAgilityPack
  - Multiple CSS/XPath selectors for robust HTML parsing
  - Polly retry policy (2 retries, exponential backoff)
  - Brazilian price format support (R$ 1.234,56)

**Scraper Selection:** O scraper é definido pelo `ProviderCategoryEnum` do provider. Os 93 providers configurados são do tipo `Cedet` (lojas OpenCart com estrutura HTML similar), por isso todos usam o `CedetSingleSearchHttpClient`. Para adicionar novos tipos de sites (ex: Amazon, MercadoLivre), basta criar um novo scraper e associá-lo à categoria correspondente no `ScraperFactory`.

**URL Templates:** Cada provider tem um `SearchUrlTemplate` que define o formato da URL de busca:
- Default (OpenCart): `index.php?route=product/search&search={search}` - usado por 89/93 providers
- WooCommerce: `?s={search}&post_type=product` - para lojas WordPress/WooCommerce
- O placeholder `{search}` é substituído pelo termo de busca URL-encoded

### Caching & Resilience

- Redis cache (optional, falls back to in-memory)
- Polly for retry policies and circuit breaker
- `ResilientScraperWrapper` for scraper resilience

**IMPORTANTE - Regra de Cache:**
- **Transactions NÃO são cacheadas** - cada transação é única e deve ser executada
- **Queries SÃO cacheadas** - resultados individuais de busca em um provider específico
- Exemplos:
  - Transação "consultar 1 livro em 3 providers" = 3 queries (cada query pode ser cacheada)
  - Transação "consultar 3 livros em 3 providers para melhor provider" = 9 queries (cada query pode ser cacheada)
- O cache é por combinação livro+provider, não por transação completa

## API Endpoints

- Swagger UI: `http://localhost:5177/swagger` (development only)

### Autenticação (públicos)
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login

### Busca de Livros (requerem autenticação)
- `GET /api/BookSearch?title={title}&isbn={isbn}` - Search book prices (título OU ISBN obrigatório)
- `POST /api/BookSearch` - Search with body (title, isbn, author, providerUrls)
- `POST /api/BookSearch/single` - Busca livro único, retorna melhor opção + 2 alternativas

### Carrinho (requerem autenticação)
- `POST /api/Cart/optimize` - Optimize cart for best prices (múltiplos providers)
- `POST /api/Cart/best-provider` - Encontra melhor provider único para todos os livros
- `GET /api/Cart/search?title={title}` - Busca preço de um livro

#### Cart Optimization Flow

O endpoint `/api/Cart/optimize` recebe uma lista de livros (ISBN + quantidade) e retorna:

1. **providerComparisons**: Tabela comparativa de TODOS os providers, ordenada por:
   - Providers com todos os livros primeiro (ordenados por menor preço total)
   - Providers parciais depois (ordenados por menor preço total)

2. **providerCarts**: Melhor opção de compra (provider com menor preço total que tem todos os livros)

3. **Métricas**: totalCost, booksCost, savings, savingsPercent, executionTimeMs, creditsUsed

**Exemplo de request:**
```json
{
  "books": [
    {"isbn": "9788535914849", "quantity": 1},
    {"isbn": "9788532530790", "quantity": 1}
  ],
  "strategy": 0,
  "maxProviders": 0,
  "includeShipping": true
}
```

**Lógica em `CartOptimizer.cs`:**
- Agrupa preços por provider
- Calcula total por provider para TODOS os livros
- Identifica quais providers têm todos os livros vs parciais
- Ordena e retorna comparação completa

### Providers (públicos)
- `GET /api/Providers` - List all providers
- `GET /api/Providers/active` - List active providers only

## Key Entry Points

- Backend: `Sherlock.Api/Program.cs`
- Frontend: `Client/src/app/app.ts` with routes in `app.routes.ts`
- Auth flow: `Sherlock.Api/Controllers/AuthController.cs` ↔ `Client/src/app/services/auth-service.ts`
- Search flow: `BookSearchController` → `W16Engine.ExecuteTransaction()` → `Scrapers`
- Cart optimization: `CartController` → `CartOptimizationService` → `CartOptimizer`

### Frontend Pages

- **SearchPage** (`Client/src/app/pages/search-page/`) - Página principal de busca e otimização de carrinho
  - Busca rápida por ISBN (mostra resultados de todos os providers)
  - Carrinho de ISBNs para otimização (encontra melhor provider único)
  - Tabela comparativa de providers com preços totais
  - Seleção de providers para busca

## Providers

93 book retailers configured in `Sherlock.Domain/Entities/Provider.cs`:
- Each has: Id, Name, Url, ProviderCategoryEnum, MinFreeShipping, BaseShippingCost, IsActive, SearchUrlTemplate
- Categories: Cedet (OpenCart stores), Amazon, MercadoLivre, etc.
- Use `Provider.AllSources` to access the static list
- SearchUrlTemplate permite configurar o formato da URL de busca por provider (default: OpenCart)
