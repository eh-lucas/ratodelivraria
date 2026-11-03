using Sherlock.Business.Core.Crawling;
using Sherlock.Business.DTOs;

namespace Sherlock.Business.Interfaces;

public interface ICatalogService
{
    /// <summary>Varre o catálogo das lojas e grava o resultado. Se <paramref name="providerIds"/>
    /// for nulo, usa todas as lojas ativas.</summary>
    Task<CatalogCrawlResult> CrawlAsync(
        IReadOnlyList<int>? providerIds,
        int? maxProviders,
        bool force = false,
        bool full = false,
        CancellationToken cancellationToken = default);

    /// <summary>Sugere títulos a partir de um trecho do nome.</summary>
    Task<List<CatalogSuggestionDto>> SuggestAsync(
        string query, int limit, CancellationToken cancellationToken = default);

    /// <summary>Título, autor e capa de cada ISBN que o sistema já conhece (no máximo 20 por chamada).</summary>
    Task<List<BookLookupDto>> LookupAsync(
        IReadOnlyList<string> isbns, CancellationToken cancellationToken = default);

    /// <summary>Descobre o ISBN abrindo a página do produto na loja, e guarda o resultado.</summary>
    Task<ResolveIsbnResultDto> ResolveIsbnAsync(
        int catalogItemId, CancellationToken cancellationToken = default);
}
