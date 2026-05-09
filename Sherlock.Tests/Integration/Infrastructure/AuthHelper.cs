using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Sherlock.Tests.Integration.Infrastructure;

// Helper compartilhado para registrar usuarios de teste e obter clients autenticados.
// Centraliza o que estava privado em AuthFlowTests para reuso pelas demais classes.
public static class AuthHelper
{
    public const string DefaultPassword = "Test1234!";

    // Email unico por chamada para evitar colisoes no banco compartilhado entre testes.
    public static string UniqueEmail() => $"user_{Guid.NewGuid():N}@test.local";

    // Registra um usuario novo e retorna o token JWT.
    public static async Task<RegisterResult> RegisterAsync(
        HttpClient client,
        string? email = null,
        string password = DefaultPassword)
    {
        email ??= UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<RegisterResult>();
        return body!;
    }

    // Cria um HttpClient ja com Authorization Bearer setado para um novo user.
    public static async Task<(HttpClient Client, RegisterResult User)> CreateAuthenticatedClientAsync(
        SherlockApiFactory factory)
    {
        var client = factory.CreateClient();
        var user = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return (client, user);
    }

    public record RegisterResult(string Token, string Email, string? Username, DateTime ExpiresAt);
}
