using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface ISingleBookSearchService
{
    /// <summary>
    /// Busca preços de um livro único, retornando a melhor opção e alternativas
    /// </summary>
    /// <param name="request">Dados da busca (título, ISBN, autor, providers)</param>
    /// <param name="userId">ID do usuário que está realizando a busca</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado com melhor opção e alternativas</returns>
    Task<SingleBookSearchResult> SearchAsync(
        SingleBookSearchRequest request,
        int userId,
        CancellationToken cancellationToken = default);
}
