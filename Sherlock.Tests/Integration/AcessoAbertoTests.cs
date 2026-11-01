using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Api.Constants;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

// Desenha a fronteira entre o que é aberto e o que exige conta.
//
// Comparar preço é o que o site faz, e faz para qualquer visitante — exigir
// cadastro para isso contradiz a própria home ("sem cadastro"). Já observados e
// histórico são de uma pessoa: o histórico mostra as buscas DELA, e servir isso a
// quem não provou quem é entregaria a busca de um visitante para outro.
//
// Sem estes testes a fronteira é só convenção: basta alguém pôr [Authorize] de
// volta na classe do BookSearchController, ou tirar o de User/Transactions, e
// nada acusa.
[Collection(nameof(IntegrationTestCollection))]
public class AcessoAbertoTests(SherlockApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // ---------- aberto a quem não tem conta ----------

    [Theory]
    [InlineData("/api/Providers")]
    [InlineData("/api/Providers/active")]
    [InlineData("/api/Catalog/suggest?q=senhor")]
    public async Task Endpoints_publicos_respondem_sem_token(string rota)
    {
        var response = await _client.GetAsync(rota);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Busca_sem_token_nao_devolve_401()
    {
        // Sem ISBN a resposta correta é 400 — o que importa aqui é que a recusa
        // seja por causa do pedido, não por falta de conta.
        var response = await _client.GetAsync("/api/BookSearch?isbn=");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- exige conta ----------

    // O carrinho É aberto, e a premissa que fechou isto era falsa.
    //
    // A versão anterior deste teste exigia token, com o argumento de que o lote é
    // um multiplicador de carga e de que "o front nem chama /api/Cart hoje". A
    // segunda parte não era verdade: a tela de resultado busca por
    // POST /cart/optimize-async, que é deste controller. Na prática o cadeado não
    // limitava lote nenhum — desligava a busca para todo visitante sem conta, num
    // site que promete "sem cadastro", e foi o que usuários relataram em
    // 01/11/2026.
    //
    // A preocupação com carga continua válida, e agora mora onde resolve: o teto
    // de MaxBooksPerOptimization, que faltava justamente no optimize-async.
    [Fact]
    public async Task Carrinho_e_aberto_para_visitante()
    {
        var response = await _client.PostAsJsonAsync("/api/Cart/optimize-async", new
        {
            books = new[] { new { isbn = "9788525406002", quantity = 1 } },
            strategy = 0,
            maxProviders = 0,
            includeShipping = false
        });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Carrinho_aberto_ainda_tem_teto_de_lote()
    {
        // O que impede o visitante de virar multiplicador de carga sobre as
        // livrarias. Sem este teto, abrir o endpoint seria trocar um problema
        // por outro.
        var demais = Enumerable.Range(0, AppConstants.Cart.MaxBooksPerOptimization + 1)
            .Select(i => new { isbn = $"97885359{i:D5}", quantity = 1 })
            .ToArray();

        var response = await _client.PostAsJsonAsync("/api/Cart/optimize-async", new
        {
            books = demais,
            strategy = 0,
            maxProviders = 0,
            includeShipping = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/Transactions")]      // histórico: as buscas de UMA pessoa
    [InlineData("/api/User/me")]           // perfil
    public async Task Conteudo_pessoal_exige_token(string rota)
    {
        var response = await _client.GetAsync(rota);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Historico_so_traz_as_buscas_de_quem_pediu()
    {
        var (clienteA, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (clienteB, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var historicoA = await clienteA.GetAsync("/api/Transactions");
        var historicoB = await clienteB.GetAsync("/api/Transactions");

        historicoA.StatusCode.Should().Be(HttpStatusCode.OK);
        historicoB.StatusCode.Should().Be(HttpStatusCode.OK);

        // Contas recém-criadas não buscaram nada. Se o histórico fosse o geral de
        // todos — como acontece quando um usuário master atende todo mundo —, estas
        // listas viriam cheias das buscas de outras pessoas.
        var listaA = await historicoA.Content.ReadFromJsonAsync<List<object>>();
        var listaB = await historicoB.Content.ReadFromJsonAsync<List<object>>();

        listaA.Should().BeEmpty();
        listaB.Should().BeEmpty();
    }
}
