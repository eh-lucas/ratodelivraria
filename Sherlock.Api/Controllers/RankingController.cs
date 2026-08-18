using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
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
    private readonly RankingSettings _settings;

    public RankingController(IQueryRepository queryRepository, IOptions<RankingSettings> settings)
    {
        _queryRepository = queryRepository;
        _settings = settings.Value;
    }

    /// <summary>Os livros mais consultados, do mais procurado para o menos.</summary>
    [HttpGet("most-searched")]
    [AllowAnonymous]
    // 60s e nao 300: a consulta custa ~0,16s e o payload e pequeno, mas quem
    // acabou de buscar um livro quer ver o ranking reagir. O servidor ja
    // recalcula a cada requisicao — este cabecalho so governa o navegador.
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> MostSearched(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var livros = await _queryRepository.GetMostSearchedAsync(
            Math.Clamp(limit, 1, 50), _settings.ResetAt, cancellationToken);

        return Ok(livros);
    }
}
