using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// Cobre o fluxo de autenticacao end-to-end: register -> login -> token -> endpoint protegido.
// Usa GET /api/User/me como endpoint protegido por nao disparar scraper externo.
[Collection(nameof(IntegrationTestCollection))]
public class AuthFlowTests(SherlockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_NewUser_Returns201WithToken()
    {
        var email = UniqueEmail();
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test1234!",
            username = "tester"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = UniqueEmail();
        await Register(email, "Test1234!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "OutraSenha123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var email = UniqueEmail();
        var password = "Test1234!";
        await Register(email, password);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = UniqueEmail();
        await Register(email, "Test1234!");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "SenhaErrada123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = UniqueEmail(),
            password = "QualquerSenha123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        var email = UniqueEmail();
        var token = await Register(email, "Test1234!");

        // Cria cliente novo pra nao vazar header entre testes
        var authedClient = factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authedClient.GetAsync("/api/User/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> Register(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    // Cada teste usa email unico pra nao colidir no banco compartilhado
    private static string UniqueEmail() => $"user_{Guid.NewGuid():N}@test.local";

    private record LoginResponse(string Token, string Email, string? Username, DateTime ExpiresAt);
}
