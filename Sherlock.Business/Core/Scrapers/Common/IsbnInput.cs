using System.Text.RegularExpressions;

namespace Sherlock.Business.Core.Scrapers.Common;

/// <summary>
/// Normaliza o ISBN que chega da API antes de ele virar 69 requisições e uma
/// linha em <c>queries.search_isbn</c> (varchar 20).
///
/// Sem isto, "ISBN 978-85-359-1484-9" (22 caracteres) fazia a busca inteira e
/// depois a gravação falhava em silêncio — nada em cache, nada no histórico — e
/// "9788535914849" e "978-85-359-1484-9" eram chaves de cache diferentes.
/// Também fecha a porta para usar a busca como amplificador: texto qualquer
/// não vale uma rodada nas lojas.
/// </summary>
public static partial class IsbnInput
{
    [GeneratedRegex(@"^(?:\d{13}|\d{9}[\dX])$")]
    private static partial Regex ValidIsbn();

    public static bool TryNormalize(string? raw, out string isbn)
    {
        isbn = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var limpo = raw.Trim().ToUpperInvariant();
        if (limpo.StartsWith("ISBN", StringComparison.Ordinal))
            limpo = limpo[4..].TrimStart(':', ' ');
        limpo = limpo.Replace("-", "").Replace(" ", "");

        if (!ValidIsbn().IsMatch(limpo)) return false;

        isbn = limpo;
        return true;
    }

    public const string ErroFormato = "ISBN inválido: informe 10 ou 13 dígitos.";
}
