using Microsoft.Extensions.DependencyInjection;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Resilience;
using Sherlock.Business.Interfaces;
using Sherlock.Business.Services;

namespace Sherlock.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookPriceService, BookPriceService>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();

        // Cache
        services.AddScoped<ICacheService, CacheService>();

        // Resilience
        services.AddScoped<ResilientScraperWrapper>();

        // Engine
        services.AddScoped<W16Engine>();

        return services;
    }
}
