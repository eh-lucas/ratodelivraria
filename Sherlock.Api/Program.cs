using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Sherlock.Data.Context;
using Sherlock.Api.Configurations;
using Sherlock.Api.Middleware;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Sherlock.Business.Core.Scrapers", Serilog.Events.LogEventLevel.Debug)
    .MinimumLevel.Override("Sherlock.Business.Core.Scrapers.Cedet.HttpClient", Serilog.Events.LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/sherlock-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Iniciando Sherlock API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    var configurator = new Configurator();

    configurator.ConfigureServices(builder);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Cache - Tenta Redis, fallback para memória
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    var useRedis = builder.Configuration.GetValue<bool>("UseRedis", false);

    if (useRedis)
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "Sherlock:";
        });
        Log.Information("Cache: Redis habilitado ({Connection})", redisConnection);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        Log.Information("Cache: Usando memória (Redis desabilitado)");
    }

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Política por usuário autenticado
        options.AddPolicy("authenticated", context =>
        {
            var userId = context.User?.FindFirst("sub")?.Value
                ?? context.User?.FindFirst("nameid")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";

            return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                TokensPerPeriod = 10,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        });

        // Política global (fallback)
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            Log.Warning("Rate limit excedido para {IP}",
                context.HttpContext.Connection.RemoteIpAddress);

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Muitas requisições. Tente novamente em alguns segundos.",
                retryAfter = 60
            }, cancellationToken);
        };
    });

    // Health checks
    var connectionString = builder.Configuration.GetConnectionString("SherlockDb");
    var healthChecks = builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString!, name: "postgresql", tags: ["db", "sql", "postgresql"]);

    if (useRedis)
    {
        healthChecks.AddRedis(redisConnection, name: "redis", tags: ["cache", "redis"]);
    }

    var app = builder.Build();

    // Aplicar migrations automaticamente
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SherlockDbContext>();
        Log.Information("Aplicando migrations do banco de dados...");
        db.Database.Migrate();
        Log.Information("Migrations aplicadas com sucesso");
    }
    
    // Primeiro do pipeline: cada log gerado a partir daqui carrega o CorrelationId
    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondido {StatusCode} em {Elapsed:0.0000}ms";
    });
    
    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseCors("AllowAngular");

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
}
finally
{
    Log.CloseAndFlush();
}

// Necessario para WebApplicationFactory<Program> nos testes de integracao
public partial class Program;
