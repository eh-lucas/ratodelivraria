using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.SearchBase.Base;
using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;

namespace SherlockAPI.Controllers
{
    public class CedetSingleAgilityController : Controller
    {
        [HttpGet("search")]
        public async Task<IActionResult> SearchBookPrice([FromQuery] CedetSingleSearchParams search)
        {
            if (string.IsNullOrEmpty(search.BookTitle))
            {
                return BadRequest("Book name is required.");
            }

            try
            {
                var requestor = new Requestor(search, SearchTypeEnum.CedetSingleAgility);
                var coreExecutor = new CoreExecutor();
                var result = await coreExecutor.ExecuteTransaction<CedetSingleAgility, CedetSingleSearchParams, CedetSingleSearchResult>(requestor);

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Logar o erro (não implementado aqui)
                return StatusCode(500, $"An error occurred while searching for book prices: {ex.Message}");
            }
            //try
            //{
            //    var consulta = new CedetSingleAgility();
            //    var result = consulta.Start();
            //    return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    // Logar o erro (não implementado aqui)
            //    return StatusCode(500, $"An error occurred while searching for book prices: {ex.Message}");
            //}
        }
    }
}
