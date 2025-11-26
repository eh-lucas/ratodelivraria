using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface ISingleBookSearchService
{
    /// <summary>
    /// Busca preços de um livro único, retornando a melhor opção e alternativas
    /// </summary>
    Task<SingleBookSearchResult> SearchAsync(
        SingleBookSearchRequest request,
        int? userId = null,
        CancellationToken cancellationToken = default);
}
