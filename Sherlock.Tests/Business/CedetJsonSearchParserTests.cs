using FluentAssertions;
using Sherlock.Business.Core.Scrapers.Cedet.Json;

namespace Sherlock.Tests.Business;

/// <summary>
/// Os payloads destes testes são recortes reais da resposta das lojas, capturados em
/// 2026-08-18. O parser é o ponto onde uma mudança silenciosa no formato da loja
/// quebraria a busca inteira sem erro de compilação — daí valer teste.
/// </summary>
public class CedetJsonSearchParserTests
{
    // Recorte real de livrariacatolicosdeverdade.com.br, busca por ISBN.
    private const string PayloadComPromocao = """
    {
      "products": [
        {
          "product_id": "35057",
          "thumb": "https://static.cedet.com.br/produtos/35057-150x226.jpg",
          "name": "Como ler livros",
          "quantity": 12,
          "ondemand": "0",
          "date_published": 1,
          "price": "R$ 124,90",
          "variants": [],
          "first_variant_type": "",
          "special": " 86,18",
          "special_percent": 31,
          "authors": [
            { "author_id": "714", "author_name": "Mortimer J. Adler" },
            { "author_id": "715", "author_name": "Charles Van Doren" }
          ],
          "href": "https://livrariacatolicosdeverdade.com.br/como-ler-livros?search=9788594090782&page=1&limit=20"
        }
      ],
      "pagination_total": 1
    }
    """;

    // Recorte real: produto sem promoção. A loja manda string vazia, não null.
    private const string PayloadSemPromocao = """
    {
      "products": [
        {
          "product_id": "38087",
          "name": "KIT - Estudos Platônicos",
          "quantity": 72,
          "price": "R$ 5.324,00",
          "special": "",
          "special_percent": false,
          "authors": [ { "author_name": "Vários autores" } ],
          "href": "https://livrariacatolicosdeverdade.com.br/kit-estudos-platonicos"
        }
      ]
    }
    """;

    [Fact]
    public void TryParse_ComPromocao_UsaOPrecoPromocional()
    {
        var candidates = CedetJsonSearchParser.TryParse(PayloadComPromocao);

        candidates.Should().NotBeNull();
        candidates!.Should().HaveCount(1);

        var book = candidates[0];
        book.Title.Should().Be("Como ler livros");
        book.Price.Should().Be(86.18m);
        book.Discount.Should().Be(31);
        book.Author.Should().Be("Mortimer J. Adler, Charles Van Doren");
    }

    [Fact]
    public void TryParse_LimpaAQueryStringDoLinkDoProduto()
    {
        var candidates = CedetJsonSearchParser.TryParse(PayloadComPromocao);

        candidates![0].ProductUrl.Should()
            .Be("https://livrariacatolicosdeverdade.com.br/como-ler-livros");
    }

    [Fact]
    public void TryParse_SemPromocao_UsaOPrecoDeTabelaESemDesconto()
    {
        var candidates = CedetJsonSearchParser.TryParse(PayloadSemPromocao);

        candidates.Should().NotBeNull();
        var book = candidates![0];
        book.Price.Should().Be(5324.00m, "o ponto é separador de milhar no formato BR");
        book.Discount.Should().Be(0);
        book.Author.Should().Be("Vários autores");
    }

    [Fact]
    public void TryParse_ListaVazia_NaoEFalha()
    {
        // A loja respondeu e não tem o livro. Diferente de não falar JSON: aqui não
        // pode cair para o HTML, senão paga-se duas requisições por loja sem estoque.
        var candidates = CedetJsonSearchParser.TryParse("""{"products": [], "pagination_total": 0}""");

        candidates.Should().NotBeNull();
        candidates!.Should().BeEmpty();
    }

    [Theory]
    [InlineData("<!DOCTYPE html><html><body>página de busca</body></html>")]
    [InlineData("Token Inválido")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("""{"erro": "sem products"}""")]
    [InlineData("""{"products": "não é lista"}""")]
    public void TryParse_RespostaForaDoFormato_DevolveNullParaCairNoHtml(string? payload)
    {
        CedetJsonSearchParser.TryParse(payload).Should().BeNull();
    }

    [Fact]
    public void TryParse_ProdutoSemNome_EIgnorado()
    {
        var payload = """
        {"products": [
          {"product_id": "1", "price": "R$ 10,00"},
          {"product_id": "2", "name": "Livro bom", "price": "R$ 20,00"}
        ]}
        """;

        var candidates = CedetJsonSearchParser.TryParse(payload);

        candidates!.Should().HaveCount(1);
        candidates[0].Title.Should().Be("Livro bom");
    }

    [Fact]
    public void TryParse_ProdutoSemPrecoUtil_EIgnorado()
    {
        var payload = """
        {"products": [
          {"product_id": "1", "name": "Sem preço", "price": "", "special": ""},
          {"product_id": "2", "name": "Com preço", "price": "R$ 20,00"}
        ]}
        """;

        var candidates = CedetJsonSearchParser.TryParse(payload);

        candidates!.Should().HaveCount(1);
        candidates[0].Title.Should().Be("Com preço");
    }

    [Fact]
    public void TryParse_PrecoNumerico_TambemEAceito()
    {
        // Não foi observado em produção, mas JSON permite; melhor não quebrar por isso.
        var payload = """{"products": [{"name": "Livro", "price": 49.9}]}""";

        var candidates = CedetJsonSearchParser.TryParse(payload);

        candidates!.Should().ContainSingle();
        candidates[0].Price.Should().Be(49.9m);
    }

    [Fact]
    public void TryParse_PromocaoMaiorQueOPrecoDeTabela_NaoInventaDescontoNegativo()
    {
        var payload = """
        {"products": [{"name": "Livro", "price": "R$ 10,00", "special": "R$ 15,00"}]}
        """;

        var candidates = CedetJsonSearchParser.TryParse(payload);

        candidates![0].Price.Should().Be(15.00m);
        candidates[0].Discount.Should().Be(0);
    }
}
