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
    public TimeSpan CacheTimeSpan { get; set; }
    public List<Source> SourcesToSearch { get; set; }

    public Requestor(SearchParameter searchParameters, List<Source> sourcesToSearch, TimeSpan cacheTimeSpan)
    {
        SearchParameters = searchParameters;
        SourcesToSearch = sourcesToSearch;
        CacheTimeSpan = cacheTimeSpan;
    }

    public Requestor(SearchParameter searchParameters)
    {
        SearchParameters = searchParameters;
        SourcesToSearch = Source.AllSources.Where(s => s.SourceCategory == SourceCategory.Cedet).ToList();
        CacheTimeSpan = TimeSpan.FromDays(1);
    }

    public Requestor()
    {
    }
}
