using API.Business.BaseLogic;
using API.Domain;
using API.Enums;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookPriceController : Controller
    {
        private readonly ConsultaBase _bookPriceScraper;

        public BookPriceController(ConsultaBase bookPriceScraper)
        {
            _bookPriceScraper = bookPriceScraper;
        }

        // Endpoint para pesquisar preços de um livro
        [HttpGet("search")]
        public async Task<IActionResult> SearchBookPrice([FromQuery] string bookName)
        {
            if (string.IsNullOrEmpty(bookName))
            {
                return BadRequest("Book name is required.");
            }

            try
            {
                List<string> inputBooks = new List<string>() { bookName };
                InputParameters input = new InputParameters(inputBooks);
                var book = await _bookPriceScraper.Execute(new Domain.Requestor(input, SearchTypeEnum.CedetSingleSearch));

                if (book == null)
                {
                    return NotFound($"No prices found for book: {bookName}");
                }

                return Ok(book);
            }
            catch (Exception ex)
            {
                // Logar o erro (não implementado aqui)
                return StatusCode(500, $"An error occurred while searching for book prices: {ex.Message}");
            }
        }
    }
}
