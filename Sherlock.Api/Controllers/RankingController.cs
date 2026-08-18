using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Api.Controllers;

/// <summary>
/// O que as pessoas mais procuram aqui. Leitura do nosso próprio histórico —
/// não consulta loja nenhuma, não custa crédito.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RankingController : ControllerBase
{
    private readonly IQueryRepository _queryRepository;

    public RankingController(IQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    /// <summary>Os livros mais consultados, do mais procurado para o menos.</summary>
    [HttpGet("most-searched")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> MostSearched(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var livros = await _queryRepository.GetMostSearchedAsync(
            Math.Clamp(limit, 1, 50), cancellationToken);

        return Ok(livros);
    }
}
