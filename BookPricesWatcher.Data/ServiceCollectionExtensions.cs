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
        services.AddDbContext<SherlockDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
