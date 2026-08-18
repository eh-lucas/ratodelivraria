namespace Sherlock.Domain.Entities;

/// <summary>
/// Um livro no ranking dos mais consultados. Não é tabela: é o resultado de
/// agregar as consultas já feitas.
/// </summary>
public class PopularBook
{
    public string Isbn { get; set; } = string.Empty;

    /// <summary>Quantas buscas distintas pediram este ISBN.</summary>
    public int Searches { get; set; }

    /// <summary>
    /// Título do livro. Vem do catálogo local quando temos, porque o título que
    /// a loja devolve na busca por ISBN às vezes é de outro livro — o scraper
    /// aceita o primeiro resultado da loja sem conferir.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Menor preço já visto para este ISBN, em qualquer loja.</summary>
    public decimal? LowestPrice { get; set; }

    public DateTime LastSearchedAt { get; set; }
}
