using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sherlock.Data.Context;
using Sherlock.Data.Repositories;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<SherlockDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookPriceRepository, BookPriceRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IQueryRepository, QueryRepository>();

        return services;
    }
}
