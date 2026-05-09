using System.Text.RegularExpressions;

namespace Sherlock.Business.Core.Scrapers.Common;

internal static partial class IsbnHelper
{
    // Captura "ISBN: 9788584911516", "ISBN9788584911516", "ISBN 978-85-849-1151-6"
    [GeneratedRegex(@"ISBN[:\s]*(\d{3}[-\s]?\d{1,5}[-\s]?\d{1,7}[-\s]?\d{1,6}[-\s]?\d{1}|\d{13}|\d{10})", RegexOptions.IgnoreCase)]
    private static partial Regex IsbnInTextRegex();

    [GeneratedRegex(@"[\s\-]")]
    private static partial Regex IsbnSeparatorsRegex();

    public static string Normalize(string isbn) =>
        IsbnSeparatorsRegex().Replace(isbn, "");

    public static bool Matches(string? extracted, string searched)
    {
        if (string.IsNullOrEmpty(extracted)) return false;
        return Normalize(extracted) == Normalize(searched);
    }

    public static string? ExtractFromText(string bodyText)
    {
        var match = IsbnInTextRegex().Match(bodyText);
        return match.Success ? Normalize(match.Groups[1].Value) : null;
    }
}
