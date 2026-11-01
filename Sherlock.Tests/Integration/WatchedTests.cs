using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sherlock.Business.Configuration;
using Sherlock.Business.DTOs;
using Sherlock.Domain.Entities;
using Sherlock.Tests.Integration.Infrastructure;

namespace Sherlock.Tests.Integration;

/// <summary>
/// A lista de observados é de uma pessoa, e o aviso vai para o e-mail dela. O que
/// estes testes seguram é a fronteira: ninguém vê nem mexe na lista de outro.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class WatchedTests(SherlockApiFactory factory)
{
    private const string Isbn = "9788535914849";
    private const string OutroIsbn = "9788532530790";

    private static object Item(string isbn, WatchRule rule = WatchRule.PercentDrop,
        decimal? alvo = null, int queda = 15, string? titulo = null)
        => new { isbn, rule = (int)rule, targetPrice = alvo, dropPercent = queda, title = titulo };

    private static async Task<WatchedBookBatchResult> AdicionarAsync(HttpClient client, params object[] itens)
    {
        var resposta = await client.PostAsJsonAsync("/api/Watched", new { items = itens });
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await resposta.Content.ReadFromJsonAsync<WatchedBookBatchResult>())!;
    }

    [Fact]
    public async Task Sem_token_a_lista_nao_abre()
    {
        var anonimo = factory.CreateClient();

        (await anonimo.GetAsync("/api/Watched")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Adiciona_e_lista()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var resultado = await AdicionarAsync(client, Item(Isbn, titulo: "O Hobbit"));

        resultado.Rejected.Should().BeEmpty();
        resultado.Items.Should().ContainSingle();

        var lista = await client.GetFromJsonAsync<List<WatchedBookDto>>("/api/Watched");

        lista.Should().ContainSingle(w => w.Isbn == Isbn);
        lista![0].Title.Should().Be("O Hobbit");
        lista[0].Active.Should().BeTrue();
    }

    [Fact]
    public async Task Pedir_o_mesmo_livro_de_novo_edita_a_regra_em_vez_de_duplicar()
    {
        // Repetir o pedido é o que a pessoa faz quando quer mudar o alvo. Criar
        // uma segunda linha faria o vigia varrer o mesmo ISBN duas vezes para ela.
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        await AdicionarAsync(client, Item(Isbn));
        await AdicionarAsync(client, Item(Isbn, WatchRule.TargetPrice, alvo: 39.90m));

        var lista = await client.GetFromJsonAsync<List<WatchedBookDto>>("/api/Watched");

        lista.Should().ContainSingle();
        lista![0].Rule.Should().Be(WatchRule.TargetPrice);
        lista[0].TargetPrice.Should().Be(39.90m);
    }

    [Fact]
    public async Task Isbn_invalido_e_recusado_sem_derrubar_o_resto_do_lote()
    {
        // Lote não é tudo-ou-nada: recusar os bons por causa de um ruim obrigaria
        // a pessoa a adivinhar qual dos dez tinha problema.
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var resultado = await AdicionarAsync(client, Item(Isbn), Item("123"));

        resultado.Items.Should().ContainSingle(i => i.Isbn == Isbn);
        resultado.Rejected.Should().ContainSingle(r => r.Isbn == "123");
    }

    [Fact]
    public async Task Pausar_e_retomar()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var criado = (await AdicionarAsync(client, Item(Isbn))).Items[0];

        var pausar = await client.PatchAsJsonAsync($"/api/Watched/{criado.Id}", new { active = false });
        pausar.StatusCode.Should().Be(HttpStatusCode.OK);

        (await pausar.Content.ReadFromJsonAsync<WatchedBookDto>())!.Active.Should().BeFalse();

        var retomar = await client.PatchAsJsonAsync($"/api/Watched/{criado.Id}", new { active = true });
        (await retomar.Content.ReadFromJsonAsync<WatchedBookDto>())!.Active.Should().BeTrue();
    }

    [Fact]
    public async Task Remove()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var criado = (await AdicionarAsync(client, Item(Isbn))).Items[0];

        (await client.DeleteAsync($"/api/Watched/{criado.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await client.GetFromJsonAsync<List<WatchedBookDto>>("/api/Watched"))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task A_lista_de_uma_pessoa_nao_aparece_para_outra()
    {
        var (a, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (b, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        await AdicionarAsync(a, Item(Isbn));
        await AdicionarAsync(b, Item(OutroIsbn));

        var listaA = await a.GetFromJsonAsync<List<WatchedBookDto>>("/api/Watched");
        var listaB = await b.GetFromJsonAsync<List<WatchedBookDto>>("/api/Watched");

        listaA.Should().ContainSingle(w => w.Isbn == Isbn);
        listaB.Should().ContainSingle(w => w.Isbn == OutroIsbn);
    }

    [Fact]
    public async Task Nao_da_para_mexer_no_livro_de_outra_pessoa()
    {
        // 404 e não 403: responder "existe, mas não é seu" já confirma que existe.
        var (a, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (b, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var deA = (await AdicionarAsync(a, Item(Isbn))).Items[0];

        (await b.PatchAsJsonAsync($"/api/Watched/{deA.Id}", new { active = false })).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        (await b.DeleteAsync($"/api/Watched/{deA.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        (await a.GetFromJsonAsync<List<WatchedBookDto>>("/api/Watched"))
            .Should().ContainSingle(w => w.Active);
    }

    [Fact]
    public async Task O_teto_por_pessoa_e_aplicado_e_diz_o_motivo()
    {
        // O teto é o único controle que existe — observar é de graça, como buscar
        // (§3.4). Ele precisa explicar a recusa, senão o livro some sem aviso.
        //
        // O número vem de WatchSettings.MaxPerUser (30 desde 01/11/2026) em vez de
        // ficar repetido aqui: teste que fixa o valor em dois lugares quebra a cada
        // ajuste do teto sem que nada tenha realmente regredido.
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var teto = new WatchSettings().MaxPerUser;

        var umAMais = Enumerable.Range(0, teto + 1)
            .Select(i => Item($"97885359{i:D5}"))
            .ToArray();

        var resultado = await AdicionarAsync(client, umAMais);

        resultado.Items.Should().HaveCount(teto);
        resultado.Rejected.Should().ContainSingle();
        resultado.Rejected[0].Reason.Should().Contain(teto.ToString());
    }
}
