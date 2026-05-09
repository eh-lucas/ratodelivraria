using System.Net;
using FluentAssertions;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// Smoke test: confirma que a API sobe, conecta no Postgres e o /health retorna Healthy.
[Collection(nameof(IntegrationTestCollection))]
public class HealthCheckTests(SherlockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}
