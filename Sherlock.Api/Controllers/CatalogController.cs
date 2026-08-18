using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.Interfaces;

namespace Sherlock.Api.Controllers;

/// <summary>
/// Catálogo local espelhado das lojas: alimenta a busca por nome.
/// As sugestões são consulta ao banco — não custam crédito nem disparam scraping.
///
/// Não há endpoint de crawl aqui de propósito. Existia um POST /crawl sem
/// autenticação: qualquer pessoa na internet podia disparar a varredura de 67
/// lojas (~2.500 requisições pesadas) contra o servidor único que todas elas
/// dividem. O <see cref="ICatalogService.CrawlAsync"/> continua existindo, mas
/// só é alcançável por dentro do processo.
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

}
