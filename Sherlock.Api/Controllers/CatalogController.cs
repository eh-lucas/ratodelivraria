using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.Interfaces;

namespace Sherlock.Api.Controllers;

/// <summary>
/// Catálogo local espelhado das lojas: alimenta a busca por nome.
/// As sugestões são consulta ao banco — não custam crédito nem disparam scraping.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>Sugere títulos a partir de um trecho do nome.</summary>
    [HttpGet("suggest")]
    [AllowAnonymous]
    public async Task<IActionResult> Suggest(
        [FromQuery] string q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var suggestions = await _catalogService.SuggestAsync(
            q ?? string.Empty, Math.Clamp(limit, 1, 25), cancellationToken);

        return Ok(suggestions);
    }

    /// <summary>Descobre o ISBN de uma sugestão abrindo a página do produto.</summary>
    [HttpPost("{id:int}/resolve-isbn")]
    [AllowAnonymous]
    public async Task<IActionResult> ResolveIsbn(int id, CancellationToken cancellationToken)
    {
        var result = await _catalogService.ResolveIsbnAsync(id, cancellationToken);
        return result.Found ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Dispara a coleta do catálogo. Operação pesada e manual — roda sob demanda,
    /// não a cada requisição.
    /// </summary>
    [HttpPost("crawl")]
    public async Task<IActionResult> Crawl(
        [FromBody] CrawlRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _catalogService.CrawlAsync(
            request?.ProviderIds,
            request?.MaxProviders,
            request?.Force ?? false,
            request?.Full ?? false,
            cancellationToken);

        return Ok(result);
    }

    public class CrawlRequest
    {
        /// <summary>Lojas específicas. Vazio = todas as ativas.</summary>
        public List<int>? ProviderIds { get; set; }

        /// <summary>Teto de lojas nesta execução — usado para rodar piloto antes da carga completa.</summary>
        public int? MaxProviders { get; set; }

        /// <summary>Ignora a janela de "varrido recentemente" e refaz tudo.</summary>
        public bool? Force { get; set; }

        /// <summary>
        /// Varre o catálogo inteiro em vez de parar quando as páginas só trazem
        /// produtos conhecidos. Mais lento e bem mais requisições.
        /// </summary>
        public bool? Full { get; set; }
    }
}
