using System.Globalization;
using System.Net;
using System.Text;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Watch;

/// <summary>Um livro que passou pela regra e vai no e-mail de hoje.</summary>
public record WatchDigestItem(WatchedBook Book, string Reason);

/// <summary>
/// Monta o resumo diário.
///
/// **Um e-mail por pessoa, com todos os livros.** Nunca um por livro: é o que
/// detona reputação de entrega e paciência ao mesmo tempo (§3.3.1). Por isso o
/// montador recebe a lista inteira de uma pessoa, e não um livro de cada vez.
///
/// Sai em HTML e em texto puro. O texto não é zelo: mensagem só-HTML pontua pior
/// nos filtros de spam, e um vigia de preço que cai na lixeira não existe.
/// </summary>
public static class WatchDigest
{
    public static string Assunto(IReadOnlyList<WatchDigestItem> itens)
    {
        if (itens.Count == 1)
            return $"Caiu de preço: {Encurtar(itens[0].Book.Title, 60)}";

        return $"{itens.Count} livros que você observa caíram de preço";
    }

    public static string Html(string? primeiroNome, IReadOnlyList<WatchDigestItem> itens, string siteUrl)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <div style="font-family:system-ui,-apple-system,'Segoe UI',sans-serif;max-width:560px;margin:0 auto;color:#1a1a1a">
            """);

        sb.Append($"<p>Oi{(string.IsNullOrWhiteSpace(primeiroNome) ? "" : ", " + E(primeiroNome))}! ");
        sb.Append(itens.Count == 1
            ? "Um livro que você observa mudou de preço:</p>"
            : $"{itens.Count} livros que você observa mudaram de preço:</p>");

        foreach (var item in itens)
        {
            var livro = item.Book;
            sb.Append("""<div style="border:1px solid #e5e5e5;border-radius:8px;padding:12px;margin:12px 0">""");

            if (!string.IsNullOrWhiteSpace(livro.ImageUrl))
                sb.Append($"""<img src="{E(livro.ImageUrl)}" alt="" width="48" style="float:left;margin-right:12px;border-radius:4px">""");

            sb.Append($"""<strong style="font-size:15px">{E(livro.Title)}</strong><br>""");

            if (!string.IsNullOrWhiteSpace(livro.Author))
                sb.Append($"""<span style="color:#666;font-size:13px">{E(livro.Author)}</span><br>""");

            sb.Append($"""<span style="font-size:20px;color:#047857;font-weight:600">{Moeda(livro.LastPrice)}</span>""");

            if (!string.IsNullOrWhiteSpace(livro.LastProviderName))
                sb.Append($""" <span style="color:#666;font-size:13px">na {E(livro.LastProviderName)}</span>""");

            sb.Append($"""<br><span style="color:#666;font-size:13px">{E(item.Reason)}</span>""");

            if (!string.IsNullOrWhiteSpace(livro.LastProductUrl))
                sb.Append($"""<br><a href="{E(livro.LastProductUrl)}" style="color:#b45309">ver na loja</a>""");

            sb.Append("""<div style="clear:both"></div></div>""");
        }

        // O preço é da loja, e muda sem nos avisar. Dizer isso no e-mail é o
        // mesmo que os Termos dizem na §3 — e aqui é onde a pessoa realmente lê.
        sb.Append("""
            <p style="color:#666;font-size:12px;line-height:1.5">
            O preço válido é o da loja no momento da compra; o que mostramos é o que
            encontramos na última consulta.
            </p>
            """);

        sb.Append($"""
            <p style="color:#666;font-size:12px">
            <a href="{E(siteUrl)}/watched" style="color:#b45309">Gerenciar meus livros observados</a> ·
            <a href="{E(siteUrl)}/watched" style="color:#666">parar de receber estes avisos</a>
            </p>
            </div>
            """);

        return sb.ToString();
    }

    public static string Texto(string? primeiroNome, IReadOnlyList<WatchDigestItem> itens, string siteUrl)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Oi{(string.IsNullOrWhiteSpace(primeiroNome) ? "" : ", " + primeiroNome)}!");
        sb.AppendLine();
        sb.AppendLine(itens.Count == 1
            ? "Um livro que você observa mudou de preço:"
            : $"{itens.Count} livros que você observa mudaram de preço:");
        sb.AppendLine();

        foreach (var item in itens)
        {
            var livro = item.Book;
            sb.AppendLine($"* {livro.Title}");
            if (!string.IsNullOrWhiteSpace(livro.Author)) sb.AppendLine($"  {livro.Author}");
            sb.Append($"  {Moeda(livro.LastPrice)}");
            sb.AppendLine(string.IsNullOrWhiteSpace(livro.LastProviderName) ? "" : $" na {livro.LastProviderName}");
            sb.AppendLine($"  {item.Reason}");
            if (!string.IsNullOrWhiteSpace(livro.LastProductUrl)) sb.AppendLine($"  {livro.LastProductUrl}");
            sb.AppendLine();
        }

        sb.AppendLine("O preço válido é o da loja no momento da compra; o que mostramos é o que");
        sb.AppendLine("encontramos na última consulta.");
        sb.AppendLine();
        sb.AppendLine($"Gerenciar ou parar os avisos: {siteUrl}/watched");

        return sb.ToString();
    }

    /// <summary>
    /// Preço sempre em pt-BR, não na cultura do servidor. Sem isto o e-mail em
    /// português sai com "R$ 39.90" dependendo de onde a API estiver rodando — e
    /// num aviso de preço é o número que a pessoa veio ler.
    /// </summary>
    private static string Moeda(decimal? v)
        => v == null ? "—" : string.Format(Brasil, "R$ {0:N2}", v.Value);

    private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Escapa para HTML. Título e autor vêm do que a loja publicou na página —
    /// texto de terceiro, que não pode ir cru para a caixa de entrada de ninguém.
    /// </summary>
    private static string E(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

    private static string Encurtar(string texto, int max)
        => texto.Length <= max ? texto : texto[..(max - 1)].TrimEnd() + "…";
}
