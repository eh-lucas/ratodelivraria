using Microsoft.AspNetCore.Mvc;
using Sherlock.Domain.Entities;

namespace SherlockAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    /// <summary>
    /// Lista todos os providers disponíveis para busca
    /// </summary>
    [HttpGet]
    public IActionResult GetProviders()
    {
        var providers = Provider.AllSources
            .Select((p, index) => new ProviderDto
            {
                Id = index + 1,
                Name = GetProviderName(p),
                Url = p.Url,
                Category = p.ProviderCategoryEnum.ToString(),
                IsActive = p.IsActive
            })
            .OrderBy(p => p.Name)
            .ToList();

        return Ok(providers);
    }

    /// <summary>
    /// Lista apenas providers ativos
    /// </summary>
    [HttpGet("active")]
    public IActionResult GetActiveProviders()
    {
        var providers = Provider.AllSources
            .Where(p => p.IsActive)
            .Select((p, index) => new ProviderDto
            {
                Id = index + 1,
                Name = GetProviderName(p),
                Url = p.Url,
                Category = p.ProviderCategoryEnum.ToString(),
                IsActive = p.IsActive
            })
            .OrderBy(p => p.Name)
            .ToList();

        return Ok(providers);
    }

    private static string GetProviderName(Provider provider)
    {
        if (!string.IsNullOrEmpty(provider.Name))
            return provider.Name;

        // Extrai nome da URL
        var uri = new Uri(provider.Url);
        var host = uri.Host.Replace("www.", "");

        // Remove extensão do domínio e formata
        var name = host.Split('.')[0];

        // Capitaliza primeira letra
        return char.ToUpper(name[0]) + name[1..];
    }
}

public class ProviderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
