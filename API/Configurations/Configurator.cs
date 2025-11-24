using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sherlock.Business;
using Sherlock.Data;

namespace SherlockAPI.Configurations;

public class Configurator
{
    private const string secretKey = "O4A*2lcuD)K;s#vmM14S";

    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var key = Encoding.ASCII.GetBytes(secretKey);

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
        builder.Services
            .AddData(builder.Configuration)
            .AddBusiness();
    }
}
