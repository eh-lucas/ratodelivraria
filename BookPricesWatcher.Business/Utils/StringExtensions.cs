using System;

namespace API.Utils;

public static class StringExtensions
{
    public static string CleanPrice(this string text)
    {
        text = text.Trim();
        if (text.Contains("R$"))
            text = text.Replace("R$", "");

        if (text.Contains(","))
            text = text.Replace(",", ".");

        text = new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());

        return text;
    }
}
