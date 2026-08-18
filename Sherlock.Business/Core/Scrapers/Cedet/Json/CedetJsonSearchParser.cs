using System.Text.Json;
using Sherlock.Business.Core.Scrapers.Common;

namespace Sherlock.Business.Core.Scrapers.Cedet.Json;

/// <summary>
/// Lê a resposta do endpoint <c>product/search/infiniteScroll</c> das lojas Cedet.
///
/// Por que existe: é a mesma busca que a página HTML faz, mas devolvendo JSON. Medido
/// em 2026-08-18 contra as 67 lojas — 0,7 KB contra 29,8 KB da página HTML, e a busca
/// completa caiu de ~45s para 11,2s. Sem regex em HTML e sem seletor CSS para quebrar.
///
/// Fica separado do cliente HTTP de propósito: parsing sem rede é parsing testável.
/// </summary>
internal static class CedetJsonSearchParser
{
    /// <summary>
    /// Converte o corpo da resposta em candidatos.
    ///
    /// Devolve <c>null</c> quando o corpo não é o JSON esperado — é o sinal de que a
    /// loja não fala esse protocolo (tema diferente, WAF na frente, WooCommerce) e o
    /// chamador deve cair no caminho HTML. Lista vazia é resposta legítima: a loja
    /// respondeu e não tem o livro.
    /// </summary>
    public static List<BookCandidate>? TryParse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;

        JsonElement root;
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
            root = document.RootElement;
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("products", out var products) ||
                products.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var candidates = new List<BookCandidate>();

            foreach (var product in products.EnumerateArray())
            {
                var candidate = ParseProduct(product);
                if (candidate != null) candidates.Add(candidate);
            }

            return candidates;
        }
    }

    private static BookCandidate? ParseProduct(JsonElement product)
    {
        var title = ReadString(product, "name");
        if (string.IsNullOrWhiteSpace(title)) return null;

        // `price` é o de tabela e `special` o promocional; 98,8% dos produtos têm
        // promoção, então o preço que vale quase sempre é o segundo.
        var listPrice = ReadPrice(product, "price");
        var specialPrice = ReadPrice(product, "special");

        var price = specialPrice > 0 ? specialPrice : listPrice;
        if (price <= 0) return null;

        var discount = specialPrice > 0 && listPrice > specialPrice
            ? (int)Math.Round(100 * (1 - (specialPrice / listPrice)))
            : 0;

        return new BookCandidate
        {
            Title = HtmlNodeExtensions.CleanText(title),
            Author = ReadAuthors(product),
            Price = price,
            Discount = discount,
            ImageUrl = ReadString(product, "thumb"),
            ProductUrl = ReadProductUrl(product),
        };
    }

    private static string ReadAuthors(JsonElement product)
    {
        if (!product.TryGetProperty("authors", out var authors) ||
            authors.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var names = new List<string>();
        foreach (var author in authors.EnumerateArray())
        {
            var name = ReadString(author, "author_name");
            if (!string.IsNullOrWhiteSpace(name)) names.Add(name.Trim());
        }

        return string.Join(", ", names);
    }

    /// <summary>
    /// O <c>href</c> vem com a query string da própria listagem
    /// (<c>?search=&amp;sort=...</c>). Guardamos só o endereço do produto.
    /// </summary>
    private static string ReadProductUrl(JsonElement product)
    {
        var href = ReadString(product, "href");
        if (string.IsNullOrWhiteSpace(href)) return string.Empty;

        var queryStart = href.IndexOf('?');
        return queryStart >= 0 ? href[..queryStart] : href;
    }

    private static decimal ReadPrice(JsonElement product, string field)
    {
        if (!product.TryGetProperty(field, out var element)) return 0;

        return element.ValueKind switch
        {
            // Sem promoção a loja manda "" ou false, não null.
            JsonValueKind.String => PriceParser.ParseBrazilian(element.GetString() ?? ""),
            JsonValueKind.Number => element.TryGetDecimal(out var value) ? value : 0,
            _ => 0,
        };
    }

    private static string? ReadString(JsonElement element, string field)
    {
        if (!element.TryGetProperty(field, out var value)) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
