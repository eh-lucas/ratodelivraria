using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Business.DTOs;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// /api/User/* requer auth. Novos users nascem com 100 creditos disponiveis (default
// das colunas em users seedado pela migration AddUserCreditsColumns) e historico vazio.
[Collection(nameof(IntegrationTestCollection))]
public class UserProfileTests(SherlockApiFactory factory)
{
    [Fact]
    public async Task Me_NewUser_Returns100AvailableCredits()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserCreditsDto>();
        body.Should().NotBeNull();
        body!.AvailableCredits.Should().Be(100);
        body.TotalCreditsUsed.Should().Be(0);
        body.UserId.Should().BeGreaterThan(0);
        body.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Credits_NewUser_MatchesMeEndpoint()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var meResponse = await client.GetAsync("/api/User/me");
        var creditsResponse = await client.GetAsync("/api/User/credits");

        creditsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await meResponse.Content.ReadFromJsonAsync<UserCreditsDto>();
        var credits = await creditsResponse.Content.ReadFromJsonAsync<UserCreditsDto>();
        credits!.AvailableCredits.Should().Be(me!.AvailableCredits);
        credits.UserId.Should().Be(me.UserId);
    }

    [Fact]
    public async Task History_NewUser_ReturnsEmptyPagedResult()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/User/credits/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CreditTransactionDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
        body.Page.Should().Be(1);
    }

    [Fact]
    public async Task History_PageSizeBeyondMax_IsClampedTo100()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        // UserController.cs:86 clampa pageSize > 100 em 100. Resposta deve refletir.
        var response = await client.GetAsync("/api/User/credits/history?page=1&pageSize=999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CreditTransactionDto>>();
        body!.PageSize.Should().Be(100);
    }
}
