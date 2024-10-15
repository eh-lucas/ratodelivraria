using API.Enums;
using Sherlock.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.SearchBase.Base;

namespace SherlockAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CedetSearchController : Controller
    {
        private readonly ConsultaBase _bookPriceScraper;

        public CedetSearchController(ConsultaBase bookPriceScraper)
        {
            _bookPriceScraper = bookPriceScraper;
        }

        // Endpoint para pesquisar preços de um livro
        [HttpGet("search")]
        public async Task<IActionResult> SearchBookPrice([FromQuery] InputParameters input)
        {
            if (string.IsNullOrEmpty(input.BookTitle))
            {
                return BadRequest("Book name is required.");
            }

            try
            {
                var requestor = new Requestor(input, SearchTypeEnum.CedetSingleSearch);
                var result = TransactionExecutor.ExecuteTransaction(requestor);

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
