using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sherlock.Business;
using Sherlock.Data;
using SherlockAPI.Services;

namespace SherlockAPI.Configurations;

public class Configurator
{
    private const string secretKey = "SherlockSuperSecretKey2024!@#$%^&*";

    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var key = Encoding.ASCII.GetBytes(secretKey);

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
            .AddBusiness();
    }
}
