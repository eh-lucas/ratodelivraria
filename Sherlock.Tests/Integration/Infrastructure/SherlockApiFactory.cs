using DotNet.Testcontainers.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Sherlock.Tests.Integration.Infrastructure;

// Sobe a API real em memoria com um Postgres efemero (Testcontainers).
// Migrations rodam no startup do Program.cs, entao o schema fica pronto para os testes.
public class SherlockApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    static SherlockApiFactory()
    {
        // Ryuk (sidecar de cleanup do Testcontainers) instavel no Docker Desktop Windows.
        // DisposeAsync ja para os containers, entao desabilitamos pra evitar falhas espurias.
        TestcontainersSettings.ResourceReaperEnabled = false;
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("sherlock_test")
        .WithUsername("sherlock_test")
        .WithPassword("sherlock_test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var connectionString = _postgres.GetConnectionString();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Override das connection strings para apontar pro container de teste
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["ConnectionStrings:SherlockDb"] = connectionString,
                // Cache em memoria (Redis nao sobe nos testes)
                ["UseRedis"] = "false",
                // appsettings.json nao traz mais segredo nenhum (a API recusa subir sem
                // chave). A mesma IConfiguration alimenta Configurator e TokenService,
                // entao assinatura e validacao usam esta chave.
                ["JwtSettings:SecretKey"] = "chave-de-teste-de-integracao-0123456789abcdef",
                // Os testes fazem dezenas de logins do mesmo IP; o teto de producao
                // (10/min) derrubaria a suite em 429.
                ["RateLimiting:AuthPerMinute"] = "100000",
                // A compra simulada de creditos fica fechada por padrao (nao ha gateway);
                // os testes do fluxo a reabrem. O teste do 501 desliga de novo.
                ["Credits:AllowSimulatedPurchase"] = "true",
            });
        });
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
