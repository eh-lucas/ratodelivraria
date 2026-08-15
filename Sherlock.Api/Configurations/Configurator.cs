using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sherlock.Business;
using Sherlock.Data;
using Sherlock.Api.Services;

namespace Sherlock.Api.Configurations;

public class Configurator
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        // Falha no startup em vez de assinar tokens com um segredo previsivel
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "JwtSettings:SecretKey nao configurada. Em desenvolvimento use " +
                "'dotnet user-secrets set \"JwtSettings:SecretKey\" \"<segredo>\"'; " +
                "em Docker defina JWT_SECRET no .env.");
        }

        // UTF-8 alinhado com TokenService (assinatura). ASCII anterior quebrava silenciosamente
        // se a SecretKey contivesse qualquer caractere fora do range ASCII.
        var key = Encoding.UTF8.GetBytes(secretKey);

        // Origens vem da config: o compose injeta Cors__AllowedOrigins__0 a partir do .env
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            allowedOrigins = ["http://localhost:4200"];
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

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
