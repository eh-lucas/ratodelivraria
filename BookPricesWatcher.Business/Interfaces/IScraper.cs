using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.Interfaces;
public interface IScraper
{
    ScraperTypeEnum ScraperType { get; }

    Task<SearchResult> ExecuteSearch(SearchParameter parameters);
}
