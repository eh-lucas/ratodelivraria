using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sherlock.Business.Interfaces;
using Sherlock.Infrastructure.Email;

namespace Sherlock.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(EmailSettings.SectionName);
        services.Configure<EmailSettings>(section);

        var enabled = section.GetValue<bool>(nameof(EmailSettings.Enabled));

        // Escolhido uma vez na inicialização, não a cada envio: assim o aviso do
        // LogEmailSender aparece no startup, onde alguém vê, e não escondido no meio do
        // primeiro login.
        if (enabled)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LogEmailSender>();

        return services;
    }
}
