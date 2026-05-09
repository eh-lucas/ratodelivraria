using System.Globalization;
using System.Text.RegularExpressions;

namespace Sherlock.Business.Core.Scrapers.Common;

internal static partial class PriceParser
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[^\d.]")]
    private static partial Regex NonNumericRegex();

    public static decimal ParseBrazilian(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText)) return 0;

        try
        {
            var text = priceText.Replace("R$", "").Replace("$", "").Trim();
            text = WhitespaceRegex().Replace(text, "");

            // Formato BR usa vírgula como decimal: "1.234,56" ou "1234,56"
            var hasBrazilianFormat = text.Contains(',') &&
                (text.LastIndexOf(',') > text.LastIndexOf('.') || !text.Contains('.'));

            text = hasBrazilianFormat
                ? text.Replace(".", "").Replace(",", ".")
                : text.Replace(",", "");

            text = NonNumericRegex().Replace(text, "");

            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0;
        }
        catch
        {
            return 0;
        }
    }
}
