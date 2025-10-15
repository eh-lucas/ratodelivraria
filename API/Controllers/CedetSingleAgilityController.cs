using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.SearchBase.Base;
using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Business.SearchBase.SearchTypes.Cedet.Agility;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;
using System.Diagnostics;

namespace SherlockAPI.Controllers
{
    public class CedetSingleAgilityController : Controller
    {
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
                var requestor = new Requestor(search, SearchTypeEnum.CedetSingleAgility);
                var coreExecutor = new CoreExecutor();
                var result = await coreExecutor.ExecuteTransaction<CedetSingleAgility, CedetSingleSearchParams, CedetSingleSearchResult>(requestor);

                stopwatch.Stop(); // para o cronômetro mesmo se ocorrer exceção
                Console.WriteLine($"⏱ Tempo total de execução: {stopwatch.ElapsedMilliseconds} ms");

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
