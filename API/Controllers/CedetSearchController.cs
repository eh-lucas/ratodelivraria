using Microsoft.AspNetCore.Authorization;
using Sherlock.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.SearchBase.Base;
using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Domain.Enums;

namespace SherlockAPI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CedetSearchController : Controller
    {
        //private readonly ConsultaBase _bookPriceScraper;

        //public CedetSearchController(ConsultaBase bookPriceScraper)
        //{
        //    _bookPriceScraper = bookPriceScraper;
        //}

        // Endpoint para pesquisar preços de um livro
        [HttpGet("search")]
        public async Task<IActionResult> SearchBookPrice([FromQuery] CedetSingleSearchParams search)
        {
            if (string.IsNullOrEmpty(search.BookTitle))
            {
                return BadRequest("Book name is required.");
            }

            try
            {
                var requestor = new Requestor(search, SearchTypeEnum.CedetSingleSearch);
                var coreExecutor = new CoreExecutor();
                var result = await coreExecutor.ExecuteTransaction(requestor);

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
