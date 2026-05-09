using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Sherlock.Business.Core.Scrapers.Common;

internal static partial class HtmlNodeExtensions
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();

    public static string? TryExtractText(this HtmlNode node, params string[] xpaths)
    {
        foreach (var xpath in xpaths)
        {
            var element = node.SelectSingleNode(xpath);
            var text = element?.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return WebUtility.HtmlDecode(text);
        }
        return null;
    }

    public static string? TryExtractHref(this HtmlNode node, params string[] xpaths)
    {
        foreach (var xpath in xpaths)
        {
            var element = node.SelectSingleNode(xpath);
            var href = element?.GetAttributeValue("href", null);
            if (!string.IsNullOrWhiteSpace(href))
                return href;
        }
        return null;
    }

    public static decimal TryExtractPrice(this HtmlNode node, params string[] xpaths)
    {
        foreach (var xpath in xpaths)
        {
            var element = node.SelectSingleNode(xpath);
            var priceText = element?.InnerText?.Trim();
            if (string.IsNullOrWhiteSpace(priceText)) continue;

            var price = PriceParser.ParseBrazilian(priceText);
            if (price > 0) return price;
        }
        return 0;
    }

    public static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return MultiSpaceRegex().Replace(text, " ").Trim();
    }
}
