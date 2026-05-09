using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Base;

/// <summary>
/// Requisição de uma transação.
/// Deve conter todas as informações para poder executar uma consulta bem-sucedida.
/// </summary>
public class Requestor
{
    public SearchParameter SearchParameters { get; set; }

    /// <summary>
    /// Tempo de cache em minutos. Se null, usa valor default do config.
    /// Cache só é aplicado para buscas por ISBN.
    /// </summary>
    public int? CacheTimeMinutes { get; set; }

    public List<Provider> SourcesToSearch { get; set; }

    public Requestor(SearchParameter searchParameters, List<Provider> sourcesToSearch, int? cacheTimeMinutes = null)
    {
        SearchParameters = searchParameters;
        SourcesToSearch = sourcesToSearch;
        CacheTimeMinutes = cacheTimeMinutes;
    }

    public Requestor(SearchParameter searchParameters)
    {
        SearchParameters = searchParameters;
        SourcesToSearch = Provider.AllSources.Where(s => s.ProviderCategoryEnum == ProviderCategoryEnum.Cedet).ToList();
    }

    public Requestor()
    {
    }
}
