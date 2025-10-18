using Microsoft.AspNetCore.Mvc;
using Sherlock.Business.Core.Scrapers;
using System.Diagnostics;
using Sherlock.Business.Core.Base;

namespace SherlockAPI.Controllers
{
    public class CedetSingleAgilityController : Controller
    {
        [HttpGet("search")]
        public async Task<IActionResult> SearchBookPrice([FromQuery] SearchParameter search)
        {
            var stopwatch = Stopwatch.StartNew(); // inicia o cronômetro

            if (string.IsNullOrEmpty(search.BookTitle))
            {
                return BadRequest("Book name is required.");
            }

            try
            {
                var requestor = new Requestor(search);
                var coreExecutor = new W16Engine();
                var result = await coreExecutor.ExecuteTransaction(requestor);

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
