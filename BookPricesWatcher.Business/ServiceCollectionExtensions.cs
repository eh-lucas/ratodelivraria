using Microsoft.Extensions.DependencyInjection;
using Sherlock.Business.Interfaces;
using Sherlock.Business.Services;

namespace Sherlock.Business;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
