# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY Sherlock.Api/*.csproj ./Sherlock.Api/
COPY Sherlock.Business/*.csproj ./Sherlock.Business/
COPY Sherlock.Data/*.csproj ./Sherlock.Data/
COPY Sherlock.Domain/*.csproj ./Sherlock.Domain/
COPY Sherlock.Infrastructure/*.csproj ./Sherlock.Infrastructure/

# Restore dependencies
RUN dotnet restore Sherlock.Api/Sherlock.Api.csproj

# Copy all source code
COPY . .

# Build the application
WORKDIR /src/Sherlock.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Copy Docker-specific appsettings from build stage
COPY --from=build /src/Sherlock.Api/appsettings.Docker.json ./appsettings.Docker.json

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

ENTRYPOINT ["dotnet", "Sherlock.Api.dll"]
