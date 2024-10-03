using System;

namespace API.Utils;

public static class StringExtensions
{
    public static string CleanPrice(this string text)
    {
        text = text.Trim();
        if (text.Contains("R$"))
            text.Replace("R$", "");

        return text.Trim();
    }
}
