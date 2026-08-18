using System.Globalization;
using System.Text.Json;

namespace Sherlock.Business.Core.Scrapers.Amazon;

/// <summary>
/// Converte o JSON que a sonda devolve de dentro da página em uma oferta.
///
/// A leitura do DOM acontece no navegador (não dá para testar em C#); o que dá
/// para testar é isto: transformar o que veio de lá em número confiável. Preço
/// da Amazon vem como texto brasileiro — "R$ 1.234,56".
/// </summary>
internal static class AmazonSearchResultParser
{
    /// <summary>
    /// Devolve null quando a busca não achou nada ou o card veio sem preço —
    /// os dois casos significam "a Amazon não tem esse livro à venda agora".
    /// </summary>
    public static AmazonOffer? TryParse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            root = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object)
            return null;

        var price = ParseBrl(Text(root, "price"));
        if (price is null or <= 0)
            return null;

        var title = Text(root, "title");
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var listPrice = ParseBrl(Text(root, "listPrice"));

        return new AmazonOffer
        {
            Asin = Text(root, "asin") ?? string.Empty,
            Title = title.Trim(),
            Price = price.Value,
            // Só é desconto quando o "De:" é maior que o preço cobrado.
            Discount = listPrice > price
                ? (int)Math.Round(100 * (1 - price.Value / listPrice.Value))
                : 0,
            Format = Text(root, "format")?.Trim(),
            ProductUrl = BuildProductUrl(Text(root, "asin")),
            ImageUrl = Text(root, "image"),
        };
    }

    /// <summary>
    /// A URL do card carrega rastreamento de sessão e expira. A forma canônica
    /// por ASIN é estável e é a que o robots.txt da Amazon libera.
    /// </summary>
    private static string? BuildProductUrl(string? asin)
        => string.IsNullOrWhiteSpace(asin) ? null : $"https://www.amazon.com.br/gp/product/{asin}";

    private static string? Text(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>"R$ 1.234,56" -> 1234.56. Ponto é milhar, vírgula é decimal.</summary>
    private static decimal? ParseBrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var digits = new string(text.Where(c => char.IsDigit(c) || c is '.' or ',').ToArray());
        if (digits.Length == 0)
            return null;

        digits = digits.Replace(".", string.Empty).Replace(',', '.');

        return decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
