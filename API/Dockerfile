# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY API/*.csproj ./API/
COPY BookPricesWatcher.Business/*.csproj ./BookPricesWatcher.Business/
COPY BookPricesWatcher.Data/*.csproj ./BookPricesWatcher.Data/
COPY BookPricesWatcher.Domain/*.csproj ./BookPricesWatcher.Domain/
COPY BookPricesWatcher.Infrastructure/*.csproj ./BookPricesWatcher.Infrastructure/

# Restore dependencies
RUN dotnet restore API/SherlockAPI.csproj

# Copy all source code
COPY . .

# Build the application
WORKDIR /src/API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Copy Docker-specific appsettings from build stage
COPY --from=build /src/API/appsettings.Docker.json ./appsettings.Docker.json

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SherlockAPI.dll"]
