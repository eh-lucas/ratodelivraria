using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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

    // O carrinho é a exceção da regra "comparar preço é aberto", e de propósito.
    // Ele não é uma busca: é até 50 buscas em lote, cada uma batendo em todos os
    // provedores. Quem compara um livro usa /api/BookSearch, que segue aberto; o
    // front nem chama /api/Cart hoje. Deixar o lote aberto seria dar a qualquer
    // visitante um multiplicador de carga sobre as livrarias que raspamos.
    //
    // Este teste existia afirmando o contrário e nunca tinha rodado — a suíte de
    // integração dependia de Docker, que faltava na máquina.
    [Fact]
    public async Task Carrinho_exige_token()
    {
        var response = await _client.PostAsJsonAsync("/api/Cart/optimize", new
        {
            books = Array.Empty<object>(),
            strategy = 0,
            maxProviders = 0,
            includeShipping = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
