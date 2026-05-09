using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Business.DTOs;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// Fluxo de compra de creditos: POST /api/Credits/purchase consome PaymentId qualquer
// (CreditService.AddCreditsAsync nao valida o conteudo, so exige nao-vazio em
// CreditsController.cs:94). Pacote 1 (Starter) adiciona 50 creditos.
[Collection(nameof(IntegrationTestCollection))]
public class CreditsPurchaseTests(SherlockApiFactory factory)
{
    [Fact]
    public async Task Purchase_ValidPackage_IncreasesBalance()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/Credits/purchase", new
        {
            packageId = 1,
            paymentId = "SIMULATED"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreditOperationResult>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        // Novo user comeca com 100. Pacote Starter adiciona >=50, entao saldo final >= 150.
        body.NewBalance.Should().BeGreaterThanOrEqualTo(150);
        body.Amount.Should().BeGreaterThan(0);
        body.TransactionId.Should().NotBeNull();
    }

    [Fact]
    public async Task Purchase_ReflectsInUserMe()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var purchaseResponse = await client.PostAsJsonAsync("/api/Credits/purchase", new
        {
            packageId = 1,
            paymentId = "SIMULATED"
        });
        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<CreditOperationResult>();

        var meResponse = await client.GetAsync("/api/User/me");
        var me = await meResponse.Content.ReadFromJsonAsync<UserCreditsDto>();

        me!.AvailableCredits.Should().Be(purchase!.NewBalance);
    }

    [Fact]
    public async Task Purchase_AppearsInHistory()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var purchaseResponse = await client.PostAsJsonAsync("/api/Credits/purchase", new
        {
            packageId = 1,
            paymentId = "SIMULATED"
        });
        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<CreditOperationResult>();

        var historyResponse = await client.GetAsync("/api/User/credits/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<PagedResult<CreditTransactionDto>>();

        history!.Items.Should().HaveCount(1);
        var tx = history.Items[0];
        tx.Amount.Should().Be(purchase!.Amount);
        tx.BalanceAfter.Should().Be(purchase.NewBalance);
    }

    [Fact]
    public async Task Purchase_InvalidPackageId_Returns400()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/Credits/purchase", new
        {
            packageId = 9999,
            paymentId = "SIMULATED"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Purchase_WithoutAuth_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Credits/purchase", new
        {
            packageId = 1,
            paymentId = "SIMULATED"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
