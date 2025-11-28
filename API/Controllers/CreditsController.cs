using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using System.Security.Claims;

namespace SherlockAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CreditsController : ControllerBase
{
    private readonly ICreditService _creditService;
    private readonly ILogger<CreditsController> _logger;

    public CreditsController(ICreditService creditService, ILogger<CreditsController> logger)
    {
        _creditService = creditService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os pacotes de créditos disponíveis para compra
    /// </summary>
    [HttpGet("packages")]
    [ProducesResponseType(typeof(List<CreditPackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackages()
    {
        try
        {
            var packages = await _creditService.GetAvailablePackagesAsync();
            return Ok(packages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar pacotes de créditos");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Erro ao listar pacotes de créditos" });
        }
    }

    /// <summary>
    /// Obtém detalhes de um pacote específico
    /// </summary>
    [HttpGet("packages/{id:int}")]
    [ProducesResponseType(typeof(CreditPackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackage(int id)
    {
        try
        {
            var package = await _creditService.GetPackageByIdAsync(id);
            if (package == null)
            {
                return NotFound(new { error = "Pacote não encontrado" });
            }
            return Ok(package);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pacote {PackageId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Erro ao obter detalhes do pacote" });
        }
    }

    /// <summary>
    /// Compra um pacote de créditos
    /// </summary>
    /// <remarks>
    /// Para ambiente de desenvolvimento/teste, use PaymentId = "SIMULATED" para simular pagamento aprovado.
    /// Em produção, integre com gateway de pagamento (Stripe, PayPal, etc.)
    /// </remarks>
    [HttpPost("purchase")]
    [Authorize]
    [ProducesResponseType(typeof(CreditOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PurchaseCredits([FromBody] PurchaseCreditsRequest request)
    {
        try
        {
            var userId = GetUserId();

            // Verifica se o pacote existe
            var package = await _creditService.GetPackageByIdAsync(request.PackageId);
            if (package == null)
            {
                return BadRequest(new { error = "Pacote de créditos inválido" });
            }

            // Em produção, aqui você validaria o pagamento com o gateway
            // Por enquanto, aceita pagamentos simulados ou com ID externo
            if (string.IsNullOrEmpty(request.PaymentId))
            {
                return BadRequest(new { error = "ID do pagamento é obrigatório" });
            }

            _logger.LogInformation(
                "Processando compra de créditos: UserId={UserId}, PackageId={PackageId}, PaymentId={PaymentId}",
                userId, request.PackageId, request.PaymentId);

            var result = await _creditService.AddCreditsAsync(userId, request.PackageId, request.PaymentId);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar compra de créditos");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Erro ao processar compra de créditos" });
        }
    }

    /// <summary>
    /// Estima o custo de uma busca antes de executá-la
    /// </summary>
    [HttpGet("estimate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult EstimateSearchCost([FromQuery] int providerCount = 10)
    {
        if (providerCount < 1) providerCount = 1;
        if (providerCount > 100) providerCount = 100;

        var estimatedCost = _creditService.EstimateSearchCost(providerCount);

        return Ok(new
        {
            providerCount,
            estimatedCost,
            description = $"Custo estimado: {estimatedCost} créditos (base + {providerCount / 2} queries bem-sucedidas estimadas)"
        });
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
