using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookPricesWatcher.Utils;

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
