using FluentAssertions;
using Sherlock.Business.Core.Scrapers.Amazon;

namespace Sherlock.Tests.Business;

/// <summary>
/// Os payloads abaixo são o que a sonda devolveu de dentro da página da Amazon
/// em 2026-08-18, não invenção: é aqui que se pega quando a Amazon mudar o card.
/// </summary>
public class AmazonSearchResultParserTests
{
    [Fact]
    public void TryParse_LeOfertaComDesconto()
    {
        var payload = """
            {"asin":"852861798X","title":"O velho e o mar","price":"R$ 35,11",
             "listPrice":"R$ 64,90","format":"Capa Comum"}
            """;

        var oferta = AmazonSearchResultParser.TryParse(payload);

        oferta.Should().NotBeNull();
        oferta!.Asin.Should().Be("852861798X");
        oferta.Title.Should().Be("O velho e o mar");
        oferta.Price.Should().Be(35.11m);
        oferta.Format.Should().Be("Capa Comum");
        // 35,11 de 64,90 é 45,9% de desconto
        oferta.Discount.Should().Be(46);
    }

    [Fact]
    public void TryParse_SemPrecoRiscadoNaoInventaDesconto()
    {
        var payload = """
            {"asin":"6585033124","title":"O mínimo sobre Platão","price":"R$ 21,93","listPrice":null}
            """;

        var oferta = AmazonSearchResultParser.TryParse(payload);

        oferta!.Price.Should().Be(21.93m);
        oferta.Discount.Should().Be(0);
    }

    [Fact]
    public void TryParse_EntendeSeparadorDeMilhar()
    {
        var payload = """{"asin":"X","title":"Coleção","price":"R$ 1.234,56"}""";

        AmazonSearchResultParser.TryParse(payload)!.Price.Should().Be(1234.56m);
    }

    [Fact]
    public void TryParse_MontaUrlCanonicaPeloAsin()
    {
        var payload = """{"asin":"8535914846","title":"1984","price":"R$ 28,92"}""";

        AmazonSearchResultParser.TryParse(payload)!.ProductUrl
            .Should().Be("https://www.amazon.com.br/gp/product/8535914846");
    }

    [Fact]
    public void TryParse_PrecoRiscadoMenorQuePrecoNaoViraDescontoNegativo()
    {
        var payload = """{"asin":"X","title":"Livro","price":"R$ 50,00","listPrice":"R$ 40,00"}""";

        AmazonSearchResultParser.TryParse(payload)!.Discount.Should().Be(0);
    }

    [Theory]
    // Busca sem nenhum card: a sonda devolve null cru.
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    // Card sem preço é livro que a Amazon não está vendendo — não é oferta.
    [InlineData("""{"asin":"X","title":"Livro","price":null}""")]
    [InlineData("""{"asin":"X","title":"Livro","price":"Ver opções de compra"}""")]
    // Card sem título não dá para mostrar a ninguém.
    [InlineData("""{"asin":"X","title":null,"price":"R$ 10,00"}""")]
    // Se vier HTML no lugar do JSON, é sinal de que a página mudou.
    [InlineData("<html>bloqueado</html>")]
    public void TryParse_DevolveNullQuandoNaoHaOferta(string? payload)
    {
        AmazonSearchResultParser.TryParse(payload).Should().BeNull();
    }
}
