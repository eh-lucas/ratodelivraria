using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Core.Base;
using Sherlock.Domain.Entities;

namespace SherlockAPI.Controllers;

/// <summary>
/// Controller para busca de preços de livros em múltiplos providers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BookSearchController : ControllerBase
{
    /// <summary>
    /// Busca preços de um livro em todos os providers ou providers específicos
    /// </summary>
    /// <param name="title">Título do livro (obrigatório)</param>
    /// <param name="isbn">ISBN do livro (opcional)</param>
    /// <param name="author">Autor do livro (opcional)</param>
    /// <param name="providerUrls">URLs dos providers separadas por vírgula (opcional)</param>
    /// <returns>Resultado da busca com preços de todos os providers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BookSearch(
        [FromQuery] string title,
        [FromQuery] string? isbn,
        [FromQuery] string? author,
        [FromQuery] string? providerUrls)
    {
        if (string.IsNullOrEmpty(title))
        {
            return BadRequest(new { error = "Título do livro é obrigatório." });
        }

        try
        {
            var parameters = new SearchParameter
            {
                BookTitle = title,
                Isbn = isbn,
                AuthorName = author,
                IsExactSearch = true
            };

            var selectedProviders = GetSelectedProviders(providerUrls);
            if (selectedProviders == null)
            {
                return BadRequest(new { error = "Nenhum provider válido encontrado com as URLs especificadas." });
            }

            var requestor = new Requestor(parameters, selectedProviders);
            var coreExecutor = new W16Engine();
            var result = await coreExecutor.ExecuteTransaction(requestor);

            return Ok(result);
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
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BookSearchPost([FromBody] BookSearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Title))
        {
            return BadRequest(new { error = "Título do livro é obrigatório." });
        }

        try
        {
            var search = new SearchParameter
            {
                BookTitle = request.Title,
                Isbn = request.Isbn,
                AuthorName = request.Author
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
            var coreExecutor = new W16Engine();
            var result = await coreExecutor.ExecuteTransaction(requestor);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = $"Erro ao buscar preços: {ex.Message}" });
        }
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
    /// Título do livro (obrigatório)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// ISBN do livro (opcional)
    /// </summary>
    public string? Isbn { get; set; }

    /// <summary>
    /// Autor do livro (opcional)
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Lista de URLs de providers específicos para buscar (opcional)
    /// </summary>
    public List<string>? ProviderUrls { get; set; }
}
