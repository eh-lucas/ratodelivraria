using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Crawling;
using Sherlock.Business.Core.Optimization;
using Sherlock.Business.Core.Prefetch;
using Sherlock.Business.Core.Progress;
using Sherlock.Business.Core.Resilience;
using Sherlock.Business.Core.Scrapers.Amazon;
using Sherlock.Business.Interfaces;
using Sherlock.Business.Services;

namespace Sherlock.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<QueryCacheSettings>(
            configuration.GetSection(QueryCacheSettings.SectionName));
        services.Configure<CatalogCrawlSettings>(
            configuration.GetSection(CatalogCrawlSettings.SectionName));
        services.Configure<SearchSettings>(
            configuration.GetSection(SearchSettings.SectionName));
        services.Configure<AmazonSettings>(
            configuration.GetSection(AmazonSettings.SectionName));
        services.Configure<PrefetchSettings>(
            configuration.GetSection(PrefetchSettings.SectionName));
        // Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookPriceService, BookPriceService>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        services.AddScoped<ICartOptimizationService, CartOptimizationService>();
        services.AddScoped<ISingleBookSearchService, SingleBookSearchService>();
        services.AddScoped<ITransactionPersistenceService, TransactionPersistenceService>();
        services.AddScoped<ICreditService, CreditService>();
        services.AddScoped<ICatalogService, CatalogService>();

        // Crawler do catálogo
        services.AddScoped<CatalogCrawler>();

        // Progresso das buscas: singleton, pois é consultado por requisições diferentes
        services.AddSingleton<SearchProgressStore>();

        // Navegador da Amazon: singleton porque subir Chrome custa ~1s e um
        // processo por busca acabaria com a memória da máquina.
        services.AddSingleton<IAmazonBrowser, AmazonBrowser>();

        // Cache
        services.AddScoped<ICacheService, CacheService>();

        // Resilience
        services.AddScoped<ResilientScraperWrapper>();

        // Engine
        services.AddScoped<W16Engine>();

        // Optimization
        services.AddScoped<CartOptimizer>();

        // Mantém os livros mais procurados quentes no cache
        services.AddHostedService<PopularBooksPrefetcher>();

        return services;
    }
}
