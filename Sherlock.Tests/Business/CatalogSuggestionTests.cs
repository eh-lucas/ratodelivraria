using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sherlock.Business.Core.Crawling;
using Sherlock.Business.Services;
using Sherlock.Data.Repositories;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Tests.Business;

public class CatalogSuggestionTests
{
    // ── Capa ────────────────────────────────────────────────────────────────

    [Fact]
    public void CedetThumbUrl_MontaAMiniaturaDoCdn()
    {
        CatalogService.CedetThumbUrl("19672").Should()
            .Be("https://static.cedet.com.br/produtos_imagem_principal_grid/19672-150x226.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12/../x")]
    public void CedetThumbUrl_IdForaDoPadrao_NaoInventaUrl(string? productId)
    {
        CatalogService.CedetThumbUrl(productId).Should().BeNull();
    }

    [Fact]
    public void ReadThumbUrl_AceitaUrlAbsolutaDaLoja()
    {
        // Formato real do campo "thumb" no infiniteScroll
        const string thumb =
            "https://static.cedet.com.br/produtos_imagem_principal_grid/19672-150x226.jpg?v=1712345678";

        CatalogCrawler.ReadThumbUrl(Json($"\"{thumb}\"")).Should().Be(thumb);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("null")]
    [InlineData("\"/image/cache/capa.jpg\"")]
    [InlineData("\"javascript:alert(1)\"")]
    [InlineData("\"data:image/png;base64,AAAA\"")]
    public void ReadThumbUrl_ValorQueNaoAbreNoNavegador_ViraNull(string json)
    {
        CatalogCrawler.ReadThumbUrl(Json(json)).Should().BeNull();
    }

    // ── LIKE ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("dom casmurro", "dom casmurro")]
    [InlineData("100%", "100\\%")]
    [InlineData("a_b", "a\\_b")]
    [InlineData("c:\\x", "c:\\\\x")]
    public void EscapeLike_TermoEhBuscadoLiteralmente(string input, string expected)
    {
        CatalogRepository.EscapeLike(input).Should().Be(expected);
    }

    // ── Sugestão ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SuggestAsync_TituloVemAntesDeAutor_ECapaDaColetaTemPreferencia()
    {
        var repo = new Mock<ICatalogRepository>();
        repo.Setup(r => r.SearchByNameAsync("machado", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CatalogItem>
            {
                // Repositório devolve primeiro quem casou pelo título
                Item(1, "A casa do Machado", "Fulano", "111"),
                Item(2, "Machado de Assis: uma biografia", "Beltrano", "222",
                    thumb: "https://static.cedet.com.br/x/222-150x226.jpg?v=9"),
                Item(3, "Dom Casmurro", "Machado de Assis", "333"),
            });

        var service = new CatalogService(
            repo.Object,
            Mock.Of<IProviderRepository>(),
            Mock.Of<IQueryRepository>(),
            new CatalogCrawler(Options.Create(new CatalogCrawlSettings()),
                NullLogger<CatalogCrawler>.Instance),
            Options.Create(new CatalogCrawlSettings()),
            NullLogger<CatalogService>.Instance);

        var result = await service.SuggestAsync("Machado", 8);

        result.Select(s => s.Title).Should().ContainInOrder(
            "Machado de Assis: uma biografia", // prefixo
            "A casa do Machado",               // contém
            "Dom Casmurro");                   // só o autor

        result[0].ThumbUrl.Should().Be("https://static.cedet.com.br/x/222-150x226.jpg?v=9");
        result[2].ThumbUrl.Should()
            .Be("https://static.cedet.com.br/produtos_imagem_principal_grid/333-150x226.jpg");
        result[2].Author.Should().Be("Machado de Assis");
    }

    [Theory]
    [InlineData("978-85-359-1484-9", "9788535914849")]
    [InlineData(" 85359 1484x ", "853591484X")]
    [InlineData("ISBN 9788535914849", "9788535914849")]
    public void NormalizeIsbn_TiraHifenEspacoERotulo(string raw, string expected)
    {
        CatalogService.NormalizeIsbn(raw).Should().Be(expected);
    }

    [Fact]
    public async Task LookupAsync_CatalogoPrimeiro_DepoisUltimaConsulta_IgnoraLixo()
    {
        var catalog = new Mock<ICatalogRepository>();
        catalog.Setup(r => r.GetByIsbnAsync("9788535914849", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Item(1, "1984", "George Orwell", "777"));
        var queries = new Mock<IQueryRepository>();
        queries.Setup(r => r.GetMetadataByIsbnAsync("9786588732120", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookMetadata("O Cinéfilo", "Walter Benjamin", "https://img/x.jpg"));

        var service = new CatalogService(catalog.Object, Mock.Of<IProviderRepository>(), queries.Object,
            new CatalogCrawler(Options.Create(new CatalogCrawlSettings()), NullLogger<CatalogCrawler>.Instance),
            Options.Create(new CatalogCrawlSettings()), NullLogger<CatalogService>.Instance);

        var found = await service.LookupAsync(new[] { "978-85-359-1484-9", "9786588732120", "abc", "0000000000000" });

        found.Should().HaveCount(2);
        found[0].Title.Should().Be("1984");
        found[0].ThumbUrl.Should().Be("https://static.cedet.com.br/produtos_imagem_principal_grid/777-150x226.jpg");
        found[1].Title.Should().Be("O Cinéfilo");
        found[1].Author.Should().Be("Walter Benjamin");
    }

    [Fact]
    public void Rank_ComparaContraOTituloNormalizado()
    {
        var item = Item(1, "História da Literatura Ocidental", null, "1");

        CatalogService.Rank(item, "historia").Should().Be(0);
        CatalogService.Rank(item, "literatura").Should().Be(1);
        CatalogService.Rank(item, "carpeaux").Should().Be(2);
    }

    private static CatalogItem Item(int id, string name, string? authors, string productId,
        string? thumb = null) => new()
    {
        Id = id,
        ProviderId = 1,
        ProductId = productId,
        Name = name,
        NameNormalized = CatalogCrawler.Normalize(name),
        Authors = authors,
        AuthorsNormalized = authors == null ? null : CatalogCrawler.Normalize(authors),
        ThumbUrl = thumb,
    };

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
