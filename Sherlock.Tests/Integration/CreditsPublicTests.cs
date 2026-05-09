using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Business.DTOs;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// Endpoints publicos do CreditsController. Os 4 pacotes sao seedados pela migration
// 20251128114817_AddUserCreditsColumns (Starter, Basico, Popular, Premium).
[Collection(nameof(IntegrationTestCollection))]
public class CreditsPublicTests(SherlockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetPackages_Returns4SeededPackages()
    {
        var response = await _client.GetAsync("/api/Credits/packages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var packages = await response.Content.ReadFromJsonAsync<List<CreditPackageDto>>();
        packages.Should().NotBeNull().And.HaveCount(4);
        packages!.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Name) && p.Credits > 0 && p.Price > 0);
    }

    [Fact]
    public async Task GetPackage_ById_ReturnsPackage()
    {
        var response = await _client.GetAsync("/api/Credits/packages/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var package = await response.Content.ReadFromJsonAsync<CreditPackageDto>();
        package.Should().NotBeNull();
        package!.Id.Should().Be(1);
        package.Name.Should().NotBeNullOrEmpty();
        package.TotalCredits.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPackage_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/Credits/packages/9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Estimate_WithValidProviderCount_ReturnsPositiveCost()
    {
        var response = await _client.GetAsync("/api/Credits/estimate?providerCount=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EstimateResponse>();
        body.Should().NotBeNull();
        body!.ProviderCount.Should().Be(10);
        body.EstimatedCost.Should().BeGreaterThan(0);
        body.Description.Should().NotBeNullOrEmpty();
    }

    private record EstimateResponse(int ProviderCount, int EstimatedCost, string Description);
}
