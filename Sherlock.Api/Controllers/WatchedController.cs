using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using System.Security.Claims;

namespace Sherlock.Api.Controllers;

/// <summary>
/// A lista de livros que cada pessoa pede para o rato vigiar.
///
/// Exige conta, e aqui não há meio-termo possível: a lista é de uma pessoa e o
/// aviso vai para o e-mail dela. Servir a lista de alguém a quem não provou quem
/// é seria entregar o que uma pessoa observa para outra.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WatchedController : ControllerBase
{
    private readonly IWatchedBookService _service;
    private readonly ILogger<WatchedController> _logger;

    public WatchedController(IWatchedBookService service, ILogger<WatchedController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>A lista de quem pediu, do mais recente para o mais antigo.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchedBookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var itens = await _service.ListAsync(GetUserId(), cancellationToken);
        return Ok(itens);
    }

    /// <summary>
    /// Adiciona em lote. Responde 200 mesmo com recusas parciais: o que foi aceito
    /// vem em <c>items</c>, o que não foi vem em <c>rejected</c> com o motivo. Um
    /// 400 global faria a pessoa perder os cinco livros que estavam certos por
    /// causa do sexto.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WatchedBookBatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add(
        [FromBody] WatchedBookBatchRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var resultado = await _service.AddAsync(GetUserId(), request, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>Muda a regra, o alvo, ou pausa/retoma.</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(WatchedBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id, [FromBody] WatchedBookPatchRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var atualizado = await _service.UpdateAsync(GetUserId(), id, request, cancellationToken);
        return atualizado == null ? NotFound() : Ok(atualizado);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        var removido = await _service.RemoveAsync(GetUserId(), id, cancellationToken);
        return removido ? NoContent() : NotFound();
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(claim, out var userId)) return userId;

        throw new UnauthorizedAccessException("UserId não encontrado no token");
    }
}
