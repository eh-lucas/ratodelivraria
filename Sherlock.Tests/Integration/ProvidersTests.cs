using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// /api/Providers e /api/Providers/active sao publicos e listam Provider.AllSources.
// A contagem segue Provider.AllSources: 83 lojas estao declaradas no arquivo, mas 16
// (estacionadas ou com DNS quebrado) estao comentadas fora da lista, sobrando 67 ativas.
// Por isso /api/Providers e /api/Providers/active devolvem o mesmo numero.
[Collection(nameof(IntegrationTestCollection))]
public class ProvidersTests(SherlockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_RetornaTodasAsLojasCadastradas()
    {
        var response = await _client.GetAsync("/api/Providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await response.Content.ReadFromJsonAsync<List<ProviderDto>>();
        providers.Should().NotBeNull().And.HaveCount(67);
        providers!.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Name));
    }

    [Fact]
    public async Task GetActive_RetornaSomenteAsAtivas()
    {
        var response = await _client.GetAsync("/api/Providers/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await response.Content.ReadFromJsonAsync<List<ProviderDto>>();
        providers.Should().NotBeNull().And.HaveCount(67);
        providers!.Should().OnlyContain(p => p.IsActive);
    }

    private record ProviderDto(int Id, string Name, string Url, string Category, bool IsActive);
}
