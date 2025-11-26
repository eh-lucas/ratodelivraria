using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Optimization;
using Sherlock.Business.Core.Resilience;
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
        // Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookPriceService, BookPriceService>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        services.AddScoped<ICartOptimizationService, CartOptimizationService>();
        services.AddScoped<ISingleBookSearchService, SingleBookSearchService>();
        services.AddScoped<ITransactionPersistenceService, TransactionPersistenceService>();

        // Cache
        services.AddScoped<ICacheService, CacheService>();

        // Resilience
        services.AddScoped<ResilientScraperWrapper>();

        // Engine
        services.AddScoped<W16Engine>();

        // Optimization
        services.AddScoped<CartOptimizer>();

        return services;
    }
}
