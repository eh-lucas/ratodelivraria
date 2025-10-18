using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers.Cedet.HttpClient;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;
public class ScraperFactory
{
    public List<IScraper> CreateScrapers(Requestor requestor)
    {
        var distinctScrapingCategories = requestor.SourcesToSearch
            .DistinctBy(s => s.ProviderCategoryEnum)
            .Select(s => s.ProviderCategoryEnum);

        var scrapers = new List<IScraper>();

        foreach (var scrapingCategory in distinctScrapingCategories)
        {
            var scraper = CreateScraper(scrapingCategory);

            if (scraper != null)
                scrapers.Add(scraper);
        }

        return scrapers;
    }

    public IScraper? CreateScraper(ProviderCategoryEnum providerCategoryEnum)
    {
        return providerCategoryEnum switch
        {
            ProviderCategoryEnum.Cedet => new CedetSingleSearchHttpClient(),
            _ => null,
        };
    }
}

