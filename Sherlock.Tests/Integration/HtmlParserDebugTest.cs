using System.Net.Http;
using HtmlAgilityPack;
using Xunit;
using Xunit.Abstractions;

namespace Sherlock.Tests.Integration;

public class HtmlParserDebugTest
{
    private readonly ITestOutputHelper _output;

    public HtmlParserDebugTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestHtmlParsing_ShouldFindProducts()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var url = "https://livrariaalexandrecosta.com.br/?s=idiota&post_type=product";
        var html = await httpClient.GetStringAsync(url);

        _output.WriteLine($"HTML Length: {html.Length}");

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Test product selector
        var products = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item-product')]");
        _output.WriteLine($"Products found: {products?.Count ?? 0}");

        Assert.NotNull(products);
        Assert.True(products.Count > 0, "Should find at least one product");

        // Parse first product
        var first = products[0];
        _output.WriteLine($"First product HTML (first 500 chars): {first.InnerHtml.Substring(0, Math.Min(500, first.InnerHtml.Length))}");

        // Test title selector
        var titleNode = first.SelectSingleNode(".//a[contains(@class, 'product-name')]");
        _output.WriteLine($"Title node: {(titleNode != null ? titleNode.InnerText.Trim() : "NULL")}");

        // Test price selector
        var priceNode = first.SelectSingleNode(".//span[contains(@class, 'price-new')]");
        _output.WriteLine($"Price node: {(priceNode != null ? priceNode.InnerText.Trim() : "NULL")}");

        // Test author selector
        var authorNode = first.SelectSingleNode(".//p[contains(@class, 'author')]");
        _output.WriteLine($"Author node: {(authorNode != null ? authorNode.InnerText.Trim() : "NULL")}");

        Assert.NotNull(titleNode);
        Assert.NotNull(priceNode);
    }
}
