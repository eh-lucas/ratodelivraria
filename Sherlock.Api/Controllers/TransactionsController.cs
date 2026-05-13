using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.Interfaces;
using System.Security.Claims;

namespace Sherlock.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IQueryHistoryService _historyService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        IQueryHistoryService historyService,
        ILogger<TransactionsController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// Lista as transações do usuário autenticado, mais recentes primeiro.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyHistory([FromQuery] int limit = 50)
    {
        try
        {
            // Limite defensivo para evitar payloads enormes
            if (limit < 1) limit = 20;
            if (limit > 200) limit = 200;

            var userId = GetUserId();
            var history = await _historyService.GetUserHistoryAsync(userId, limit);
            return Ok(history);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter histórico de transações");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Erro ao obter histórico de transações" });
        }
    }

    /// <summary>
    /// Detalhes de uma transação específica (queries individuais por provider).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionDetail(int id)
    {
        try
        {
            var userId = GetUserId();
            var detail = await _historyService.GetTransactionDetailAsync(id);
            if (detail == null) return NotFound();

            // Bloqueia acesso a transações de outros usuários
            if (!await UserOwnsTransactionAsync(userId, id))
                return NotFound();

            return Ok(detail);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes da transação {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Erro ao obter detalhes da transação" });
        }
    }

    private async Task<bool> UserOwnsTransactionAsync(int userId, int transactionId)
    {
        // Reusa o histórico paginado para conferir posse — evita query extra
        var history = await _historyService.GetUserHistoryAsync(userId, 200);
        return history.Any(t => t.Id == transactionId);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("UserId não encontrado no token");
    }
}
