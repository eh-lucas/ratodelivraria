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
using Sherlock.Business.Core.Watch;
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
        services.Configure<RankingSettings>(
            configuration.GetSection(RankingSettings.SectionName));
        services.Configure<CartRequerySettings>(
            configuration.GetSection(CartRequerySettings.SectionName));
        services.Configure<WatchSettings>(
            configuration.GetSection(WatchSettings.SectionName));
        // Services
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookPriceService, BookPriceService>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        services.AddScoped<ICartOptimizationService, CartOptimizationService>();
        services.AddScoped<ISingleBookSearchService, SingleBookSearchService>();
        services.AddScoped<ITransactionPersistenceService, TransactionPersistenceService>();
        services.AddScoped<ICreditService, CreditService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IWatchedBookService, WatchedBookService>();

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

        // Teto global de requisicoes as livrarias, compartilhado por todas as
        // buscas: singleton de proposito, e o estado que precisa ser unico.
        services.AddSingleton<StoreRequestGate>();

        // Engine
        services.AddScoped<W16Engine>();

        // Optimization
        services.AddScoped<CartOptimizer>();

        // Quem decide de quem é a vez entre o aquecedor e a busca de gente.
        // Singleton pelo mesmo motivo do portão acima: o estado tem que ser único.
        services.AddSingleton<SearchPriority>();

        // Mantém os livros mais procurados quentes no cache
        services.AddHostedService<PopularBooksPrefetcher>();

        // O vigia de preço: varre de madrugada, avisa de manhã. Os dois nascem
        // desligados (Watch:SweepEnabled / Watch:NotifyEnabled) — ligar isso manda
        // requisição para as lojas e e-mail para gente de verdade.
        // A varredura de um ISBN serve aos dois caminhos: a rodada noturna e a
        // primeira varredura de quem acabou de observar o livro. Singleton porque
        // os dois consumidores sao hosted services, e porque ele so depende de
        // singletons — o DbContext vem do escopo que ele mesmo abre por varredura.
        services.AddSingleton<WatchSweepRunner>();

        // Fila singleton: o POST escreve nela e devolve na hora; o worker drena.
        services.AddSingleton<ImmediateSweepQueue>();
        services.AddHostedService<ImmediateSweepWorker>();

        services.AddHostedService<WatchSweepWorker>();
        services.AddHostedService<WatchNotifyWorker>();

        return services;
    }
}
