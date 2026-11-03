using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sherlock.Business;
using Sherlock.Data;
using Sherlock.Infrastructure;
using Sherlock.Api.Services;

namespace Sherlock.Api.Configurations;

public class Configurator
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
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

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // A chave é lida da IConfiguration final, não de builder.Configuration no
        // topo do Program: assim o override dos testes de integração (que entra
        // depois do Program rodar) vale tanto para assinar quanto para validar.
        //
        // Sem segredo não sobe (ValidateOnStart). O fallback que existia aqui era
        // uma chave fixa, publicada no repositório: bastava a variável faltar para
        // qualquer pessoa assinar um token de qualquer usuário. 32 caracteres é o
        // mínimo do HMAC-SHA256 com folga.
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var jwt = configuration.GetSection("JwtSettings");
                var secretKey = jwt["SecretKey"] ?? string.Empty;

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    // UTF-8 alinhado com TokenService; chave vazia vira 1 byte só
                    // para o objeto existir — a validação abaixo recusa a subida.
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretKey.Length > 0 ? secretKey : "\0")),
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
                // Token só no header Authorization. Aceitar `?token=` na URL fazia o
                // token parar nos logs do nginx e da Cloudflare e no histórico do
                // navegador; nenhum cliente nosso usava isso.
            })
            .Validate(
                options => options.TokenValidationParameters.IssuerSigningKey is SymmetricSecurityKey { Key.Length: >= 32 },
                "JwtSettings:SecretKey ausente ou com menos de 32 caracteres. " +
                "Defina JWT_SECRET no .env (produção) ou em appsettings.Development.json (dev).")
            .ValidateOnStart();

        RegisterServices(builder);

        builder.Services.AddControllers();
    }

    static void RegisterServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<TokenService>();

        builder.Services.Configure<GoogleAuthSettings>(
            builder.Configuration.GetSection(GoogleAuthSettings.SectionName));
        builder.Services.AddScoped<UsernameService>();
        builder.Services.AddScoped<GoogleLoginService>();
        builder.Services.AddScoped<RefreshTokenService>();

        builder.Services
            .AddData(builder.Configuration)
            .AddBusiness(builder.Configuration)
            .AddInfrastructure(builder.Configuration);
    }
}
