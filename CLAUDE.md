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

## Architecture

The solution follows Clean Architecture with these layers:

- **BookPricesWatcher.Domain/** - Entities (User, Book, BookPrice, Provider, Query, Scraper, Token) and repository interfaces
- **BookPricesWatcher.Business/** - Business logic, services, scrapers (W16Engine), DTOs
- **BookPricesWatcher.Data/** - EF Core DbContext (SherlockDbContext), repositories, migrations
- **BookPricesWatcher.Infrastructure/** - Cross-cutting concerns
- **API/** - ASP.NET Core Web API controllers, JWT authentication (TokenService), DI configuration
- **Client/** - Angular 20 app with standalone components pattern

### Key Patterns

- Repository pattern for data access (IUserRepository → UserRepository)
- DI registration via extension methods on IServiceCollection in each layer
- JWT Bearer authentication configured in `API/Configurations/Configurator.cs`
- Angular auth service manages tokens in localStorage with @auth0/angular-jwt

### Database

PostgreSQL connection configured in `API/appsettings.json`. DbContext: `BookPricesWatcher.Data/Context/SherlockDbContext.cs`

### Web Scraping

Uses HtmlAgilityPack and Selenium WebDriver. Core engine in `BookPricesWatcher.Business/Core/W16Engine.cs`.

## API Endpoints

- Swagger UI: `http://localhost:5177/swagger` (development only)
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `GET /book-search?BookTitle={title}` - Search book prices

## Key Entry Points

- Backend: `API/Program.cs`
- Frontend: `Client/src/app/app.ts` with routes in `app.routes.ts`
- Auth flow: `API/Controllers/AuthController.cs` ↔ `Client/src/app/services/auth-service.ts`
