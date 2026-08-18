using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers.Amazon;
using Sherlock.Business.Core.Scrapers.Cedet.HttpClient;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.Core.Scrapers;
public class ScraperFactory
{
    private readonly ILoggerFactory _loggerFactory;

    // A Amazon precisa de um navegador de pé, que e um singleton do processo.
    // Sem ele a categoria Amazon simplesmente nao produz scraper e o motor
    // registra a loja como falha, sem derrubar a busca.
    private readonly IAmazonBrowser? _amazonBrowser;

    public ScraperFactory() : this(NullLoggerFactory.Instance)
    {
    }

    public ScraperFactory(ILoggerFactory loggerFactory, IAmazonBrowser? amazonBrowser = null)
    {
        _loggerFactory = loggerFactory;
        _amazonBrowser = amazonBrowser;
    }

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
            ProviderCategoryEnum.Cedet => new CedetSingleSearchHttpClient(
                _loggerFactory.CreateLogger<CedetSingleSearchHttpClient>()),
            ProviderCategoryEnum.Amazon when _amazonBrowser is not null => new AmazonBrowserScraper(
                _amazonBrowser, _loggerFactory.CreateLogger<AmazonBrowserScraper>()),
            _ => null,
        };
    }
}

