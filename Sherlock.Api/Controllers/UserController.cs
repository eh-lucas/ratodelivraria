using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using System.Security.Claims;

namespace Sherlock.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ICreditService _creditService;
    private readonly ILogger<UserController> _logger;

    public UserController(ICreditService creditService, ILogger<UserController> logger)
    {
        _creditService = creditService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém informações do usuário atual, incluindo saldo de créditos
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserCreditsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetUserId();
            var userCredits = await _creditService.GetUserCreditsAsync(userId);
            return Ok(userCredits);
        }
        // Mantido: traduz "usuário inexistente" para 404 — semântica que o handler global não tem
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Usuário não encontrado ao buscar informações");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém o saldo de créditos do usuário atual
    /// </summary>
    [HttpGet("credits")]
    [ProducesResponseType(typeof(UserCreditsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCredits()
    {
        var userId = GetUserId();
        var credits = await _creditService.GetUserCreditsAsync(userId);
        return Ok(credits);
    }

    /// <summary>
    /// Obtém o histórico de transações de créditos do usuário
    /// </summary>
    [HttpGet("credits/history")]
    [ProducesResponseType(typeof(PagedResult<CreditTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreditHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var history = await _creditService.GetCreditHistoryAsync(userId, page, pageSize);
        return Ok(history);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("UserId não encontrado no token");
    }
}
