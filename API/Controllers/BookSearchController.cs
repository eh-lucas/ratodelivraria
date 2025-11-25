using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Core.Base;
using Sherlock.Domain.Entities;

namespace SherlockAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookSearchController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> BookSearch(
        [FromQuery] string title,
        [FromQuery] string? isbn,
        [FromQuery] string? author,
        [FromQuery] string? providerUrls) // URLs separadas por vírgula
    {
        if (string.IsNullOrEmpty(title))
        {
            return BadRequest("Book title is required.");
        }

        try
        {
            var search = new SearchParameter
            {
                BookTitle = title,
                Isbn = isbn,
                AuthorName = author
            };

            // Filtra providers se especificados
            List<Provider> selectedProviders;
            if (!string.IsNullOrEmpty(providerUrls))
            {
                var urls = providerUrls.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .ToHashSet();

                selectedProviders = Provider.AllSources
                    .Where(p => urls.Contains(p.Url))
                    .ToList();

                if (selectedProviders.Count == 0)
                {
                    return BadRequest("No valid providers found with the specified URLs.");
                }
            }
            else
            {
                // Usa todos os providers ativos
                selectedProviders = Provider.AllSources.Where(p => p.IsActive).ToList();
            }

            var requestor = new Requestor(search, selectedProviders);
            var coreExecutor = new W16Engine();
            var result = await coreExecutor.ExecuteTransaction(requestor);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while searching for book prices: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> BookSearchPost([FromBody] BookSearchRequest request)
    {
        if (string.IsNullOrEmpty(request.Title))
        {
            return BadRequest("Book title is required.");
        }

        try
        {
            var search = new SearchParameter
            {
                BookTitle = request.Title,
                Isbn = request.Isbn,
                AuthorName = request.Author
            };

            // Filtra providers se especificados
            List<Provider> selectedProviders;
            if (request.ProviderUrls != null && request.ProviderUrls.Count > 0)
            {
                var urls = request.ProviderUrls.ToHashSet();
                selectedProviders = Provider.AllSources
                    .Where(p => urls.Contains(p.Url))
                    .ToList();

                if (selectedProviders.Count == 0)
                {
                    return BadRequest("No valid providers found with the specified URLs.");
                }
            }
            else
            {
                // Usa todos os providers ativos
                selectedProviders = Provider.AllSources.Where(p => p.IsActive).ToList();
            }

            var requestor = new Requestor(search, selectedProviders);
            var coreExecutor = new W16Engine();
            var result = await coreExecutor.ExecuteTransaction(requestor);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while searching for book prices: {ex.Message}");
        }
    }
}

public class BookSearchRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Author { get; set; }
    public List<string>? ProviderUrls { get; set; }
}
