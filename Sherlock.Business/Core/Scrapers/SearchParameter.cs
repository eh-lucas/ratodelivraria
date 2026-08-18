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
}
