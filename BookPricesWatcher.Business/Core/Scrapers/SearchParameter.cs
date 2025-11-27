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
}
