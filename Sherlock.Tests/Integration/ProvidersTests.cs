using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// /api/Providers e /api/Providers/active sao publicos e listam Provider.AllSources
// (93 providers estaticos no codigo, todos ativos por padrao). Resposta deterministica.
[Collection(nameof(IntegrationTestCollection))]
public class ProvidersTests(SherlockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Returns93Providers()
    {
        var response = await _client.GetAsync("/api/Providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await response.Content.ReadFromJsonAsync<List<ProviderDto>>();
        providers.Should().NotBeNull().And.HaveCount(93);
        providers!.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Name));
    }

    [Fact]
    public async Task GetActive_Returns93Providers_AllActive()
    {
        var response = await _client.GetAsync("/api/Providers/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await response.Content.ReadFromJsonAsync<List<ProviderDto>>();
        providers.Should().NotBeNull().And.HaveCount(93);
        providers!.Should().OnlyContain(p => p.IsActive);
    }

    private record ProviderDto(int Id, string Name, string Url, string Category, bool IsActive);
}
