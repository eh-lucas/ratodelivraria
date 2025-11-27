using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using System.Security.Claims;

namespace SherlockAPI.Controllers;

/// <summary>
/// Controller para busca de preços de livros em múltiplos providers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookSearchController : ControllerBase
{
    private readonly W16Engine _engine;
    private readonly ISingleBookSearchService _singleBookSearchService;

    public BookSearchController(W16Engine engine, ISingleBookSearchService singleBookSearchService)
    {
        _engine = engine;
        _singleBookSearchService = singleBookSearchService;
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

    /// <summary>
    /// Busca preços de um livro em todos os providers ou providers específicos
    /// </summary>
    /// <param name="isbn">ISBN do livro (obrigatório)</param>
    /// <param name="providerUrls">URLs dos providers separadas por vírgula (opcional)</param>
    /// <returns>Resultado da busca com preços de todos os providers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(BookSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BookSearch(
        [FromQuery] string isbn,
        [FromQuery] string? providerUrls)
    {
        if (string.IsNullOrEmpty(isbn))
        {
            return BadRequest(new { error = "ISBN do livro é obrigatório." });
        }

        try
        {
            var parameters = new SearchParameter
            {
                Isbn = isbn
            };

            var selectedProviders = GetSelectedProviders(providerUrls);
            if (selectedProviders == null)
            {
                return BadRequest(new { error = "Nenhum provider válido encontrado com as URLs especificadas." });
            }

            var requestor = new Requestor(parameters, selectedProviders);
            var userId = GetUserId();
            var result = await _engine.ExecuteTransaction(requestor, userId);

            var response = BookSearchResponseDto.FromSearchResult(result, isbn);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = $"Erro ao buscar preços: {ex.Message}" });
        }
    }

    /// <summary>
    /// Busca preços de um livro via POST (permite payloads maiores)
    /// </summary>
    /// <param name="request">Dados da busca</param>
    /// <returns>Resultado da busca com preços de todos os providers</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BookSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BookSearchPost([FromBody] BookSearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Isbn))
        {
            return BadRequest(new { error = "ISBN do livro é obrigatório." });
        }

        try
        {
            var search = new SearchParameter
            {
                Isbn = request.Isbn
            };

            List<Provider> selectedProviders;
            if (request.ProviderUrls != null && request.ProviderUrls.Count > 0)
            {
                var urls = request.ProviderUrls.ToHashSet();
                selectedProviders = Provider.AllSources
                    .Where(p => urls.Contains(p.Url))
                    .ToList();

                if (selectedProviders.Count == 0)
                {
                    return BadRequest(new { error = "Nenhum provider válido encontrado com as URLs especificadas." });
                }
            }
            else
            {
                selectedProviders = Provider.AllSources.Where(p => p.IsActive).ToList();
            }

            var requestor = new Requestor(search, selectedProviders);
            var userId = GetUserId();
            var result = await _engine.ExecuteTransaction(requestor, userId);

            var response = BookSearchResponseDto.FromSearchResult(result, request.Isbn);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = $"Erro ao buscar preços: {ex.Message}" });
        }
    }

    /// <summary>
    /// Busca preços de um livro único, retornando melhor opção e 2 alternativas
    /// </summary>
    /// <param name="request">Dados da busca</param>
    /// <returns>Melhor opção e alternativas</returns>
    [HttpPost("single")]
    [ProducesResponseType(typeof(SingleBookSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SingleBookSearch([FromBody] SingleBookSearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Isbn))
        {
            return BadRequest(new { error = "ISBN do livro é obrigatório." });
        }

        var userId = GetUserId();
        var result = await _singleBookSearchService.SearchAsync(request, userId);
        return Ok(result);
    }

    private static List<Provider>? GetSelectedProviders(string? providerUrls)
    {
        if (string.IsNullOrEmpty(providerUrls))
        {
            return Provider.AllSources.Where(p => p.IsActive).ToList();
        }

        var urls = providerUrls.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .ToHashSet();

        var providers = Provider.AllSources
            .Where(p => urls.Contains(p.Url))
            .ToList();

        return providers.Count == 0 ? null : providers;
    }
}

/// <summary>
/// Request para busca de livros via POST
/// </summary>
public class BookSearchRequest
{
    /// <summary>
    /// ISBN do livro (obrigatório)
    /// </summary>
    public string Isbn { get; set; } = string.Empty;

    /// <summary>
    /// Lista de URLs de providers específicos para buscar (opcional)
    /// </summary>
    public List<string>? ProviderUrls { get; set; }
}
