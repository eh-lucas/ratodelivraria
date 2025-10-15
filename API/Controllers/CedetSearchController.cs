using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.SearchBase.Base;
using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Business.SearchBase.SearchTypes.Cedet.HttpClient;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;
using System.Diagnostics;

namespace SherlockAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CedetSearchController : Controller
    {
        // Endpoint para pesquisar preços de um livro
        [HttpGet("search")]
        public async Task<IActionResult> SearchBookPrice([FromQuery] CedetSingleSearchParams search)
        {
            var stopwatch = Stopwatch.StartNew(); // inicia o cronômetro

            if (string.IsNullOrEmpty(search.BookTitle))
            {
                return BadRequest("Book name is required.");
            }

            try
            {
                var requestor = new Requestor(search, SearchTypeEnum.CedetSingleSearch);
                var coreExecutor = new CoreExecutor();
                var result = await coreExecutor.ExecuteTransaction<CedetSingleSearch, CedetSingleSearchParams, CedetSingleSearchResult>(requestor);

                stopwatch.Stop(); // para o cronômetro mesmo se ocorrer exceção
                Console.WriteLine($"⏱ Tempo total de execução: {stopwatch.ElapsedMilliseconds} ms");

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Logar o erro (não implementado aqui)
                return StatusCode(500, $"An error occurred while searching for book prices: {ex.Message}");
            }
        }
        [HttpGet("httpclient")]
        public async Task<IActionResult> SearchBookPriceHttp([FromQuery] CedetSingleSearchParams search)
        {
            var stopwatch = Stopwatch.StartNew(); // inicia o cronômetro

            if (string.IsNullOrEmpty(search.BookTitle))
            {
                return BadRequest("Book name is required.");
            }

            try
            {
                var requestor = new Requestor(search, SearchTypeEnum.CedetSingleAgilityHttpClient);
                var coreExecutor = new CoreExecutor();
                var result = await coreExecutor.ExecuteTransaction<CedetSingleSearchHttpClient, CedetSingleSearchParams, CedetSingleSearchResult>(requestor);

                stopwatch.Stop(); // para o cronômetro mesmo se ocorrer exceção
                Console.WriteLine($"⏱ Tempo total de execução: {stopwatch.ElapsedMilliseconds} ms");
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Logar o erro (não implementado aqui)
                return StatusCode(500, $"An error occurred while searching for book prices: {ex.Message}");
            }
        }
    }
}
