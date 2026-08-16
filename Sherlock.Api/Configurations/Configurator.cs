using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sherlock.Business;
using Sherlock.Data;
using Sherlock.Api.Authentication;
using Sherlock.Api.Services;

namespace Sherlock.Api.Configurations;

public class Configurator
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "SherlockSuperSecretKey2024!@#$%^&*";
        // UTF-8 alinhado com TokenService (assinatura). ASCII anterior quebrava silenciosamente
        // se a SecretKey contivesse qualquer caractere fora do range ASCII.
        var key = Encoding.UTF8.GetBytes(secretKey);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        // Modo demo (branch de apresentação): autentica toda requisição como o
        // usuário master, sem login. Default off — produção segue com JWT.
        var demoMode = builder.Configuration.GetSection(DemoModeOptions.SectionName).Get<DemoModeOptions>()
                       ?? new DemoModeOptions();
        builder.Services.Configure<DemoModeOptions>(builder.Configuration.GetSection(DemoModeOptions.SectionName));

        if (demoMode.Enabled)
        {
            builder.Services.AddSingleton(new DemoMasterUser { Email = demoMode.MasterUserEmail });

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = DemoAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = DemoAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
                    DemoAuthenticationHandler.SchemeName, _ => { });
        }
        else
        {
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["token"];
                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }

        RegisterServices(builder);

        builder.Services.AddControllers();
    }

    static void RegisterServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<TokenService>();

        builder.Services
            .AddData(builder.Configuration)
            .AddBusiness(builder.Configuration);
    }
}
