using FluentAssertions;
using Sherlock.Business.Core.Watch;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Business;

public class WatchDigestTests
{
    private static WatchedBook Livro(string titulo, decimal preco, string? autor = null)
        => new()
        {
            Isbn = "9788535914849",
            Title = titulo,
            Author = autor,
            LastPrice = preco,
            LastProviderName = "Livraria X",
            LastProductUrl = "https://loja.com.br/p/1",
        };

    [Fact]
    public void Um_livro_vai_no_assunto()
    {
        var assunto = WatchDigest.Assunto(new[] { new WatchDigestItem(Livro("O Hobbit", 39.90m), "caiu") });

        assunto.Should().Contain("O Hobbit");
    }

    [Fact]
    public void Varios_livros_viram_um_assunto_so()
    {
        // Um e-mail por pessoa, nunca um por livro: é o que detona reputação de
        // entrega e paciência ao mesmo tempo (§3.3.1).
        var itens = new[]
        {
            new WatchDigestItem(Livro("O Hobbit", 39.90m), "caiu"),
            new WatchDigestItem(Livro("Dom Casmurro", 19.90m), "caiu"),
        };

        WatchDigest.Assunto(itens).Should().Be("2 livros que você observa caíram de preço");
    }

    [Fact]
    public void Titulo_da_loja_nao_vai_cru_para_o_html()
    {
        // Título e autor saem da página da loja — texto de terceiro. Um `<script>`
        // num título chegaria à caixa de entrada de uma pessoa de verdade.
        var item = new WatchDigestItem(
            Livro("<script>alert(1)</script>", 10m, autor: "Fulano & Cia"), "caiu");

        var html = WatchDigest.Html("Lucas", new[] { item }, "https://ratodelivraria.com.br");

        html.Should().NotContain("<script>");
        html.Should().Contain("&lt;script&gt;");
        html.Should().Contain("Fulano &amp; Cia");
    }

    [Fact]
    public void Texto_puro_tem_o_essencial()
    {
        // Mensagem só-HTML pontua pior nos filtros de spam, e um aviso de preço
        // que cai na lixeira não existe.
        var texto = WatchDigest.Texto(
            "Lucas",
            new[] { new WatchDigestItem(Livro("O Hobbit", 39.90m), "é o menor preço dos últimos 30 dias") },
            "https://ratodelivraria.com.br");

        texto.Should().Contain("O Hobbit");
        texto.Should().Contain("39,90");
        texto.Should().Contain("Livraria X");
        texto.Should().Contain("https://ratodelivraria.com.br/watched");
    }

    [Fact]
    public void Sem_nome_o_email_nao_fica_com_virgula_solta()
    {
        var html = WatchDigest.Html(null, new[] { new WatchDigestItem(Livro("X", 1m), "caiu") }, "https://x");

        html.Should().Contain("Oi!");
        html.Should().NotContain("Oi, !");
    }
}
