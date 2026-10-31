using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;

/// <summary>
/// Parâmetros de busca - apenas ISBN é usado como termo de consulta
/// </summary>
public class SearchParameter
{
    /// <summary>
    /// ISBN do livro (parâmetro obrigatório de busca)
    /// </summary>
    public string Isbn { get; set; } = string.Empty;

    /// <summary>
    /// Provider onde será feita a busca
    /// </summary>
    public Provider? Source { get; set; }

    /// <summary>
    /// Sinaliza que a busca faz parte de um carrinho (múltiplos livros agrupados).
    /// Usado apenas para distinguir transações no histórico — não afeta o scraping.
    /// </summary>
    public bool IsCart { get; set; }

    /// <summary>
    /// Marca a busca disparada pelo aquecedor de cache, não por uma pessoa.
    ///
    /// Precisa ficar registrado porque o ranking de mais consultados conta
    /// transações: sem a marca, o aquecedor votaria nos próprios livros e o
    /// ranking congelaria nos dez primeiros para sempre.
    /// </summary>
    public bool IsPrefetch { get; set; }

    /// <summary>
    /// Teto de tempo desta requisição, quando ela precisa de um diferente do
    /// padrão da loja. Usado pela repescagem do carrinho, que tem orçamento
    /// próprio e bem mais curto.
    /// </summary>
    public int? RequestTimeoutSeconds { get; set; }

    /// <summary>
    /// Desliga o retry do scraper.
    ///
    /// A repescagem já é a segunda tentativa daquela loja. Deixar o retry ligado
    /// dentro dela devolveria o teto de 20s pela porta dos fundos — justamente o
    /// que ela existe para evitar.
    /// </summary>
    public bool SkipRetry { get; set; }
}
