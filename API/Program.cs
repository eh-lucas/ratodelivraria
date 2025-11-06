using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using SherlockAPI.Configurations;

var builder = WebApplication.CreateBuilder(args);

// JWT
var authenticationConfig = new AuthenticationConfig();
authenticationConfig.ConfigureServices(builder.Services);

// Add services to the container.
builder.Services.AddControllers();
//builder.Services.AddScoped<ConsultaBase<CedetSingleSearchParams, CedetSingleSearchResult>, CedetSingleSearch>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SherlockDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
