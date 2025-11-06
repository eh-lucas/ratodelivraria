using HtmlAgilityPack;
using Sherlock.Domain.Entities;
using System.Globalization;
using API.Utils;
using Sherlock.Domain.Enums;
using Sherlock.Business.Interfaces;

namespace Sherlock.Business.Core.Scrapers.Cedet.HttpClient
{
    public class CedetSingleSearchHttpClient : IScraper
    {

        private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";

        public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetSingleAgilityHttpClient;

        public async Task<BookPriceResult> ExecuteSearch(SearchParameter parameters)
        {
            try
            {
                var website = parameters.Source.Url;
                var bookTitle = parameters.BookTitle;

                Console.WriteLine($"Consultando livro: {bookTitle} em {website}");
                string searchTerm = Uri.EscapeDataString(parameters.BookTitle);
                string url = $"{parameters.Source.Url}index.php?route=product/search&search={searchTerm}";

                using var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                var response = await http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Erro ao buscar página: {response.StatusCode}");

                var html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var inputNode = doc.DocumentNode.SelectSingleNode(GridXPath);
                if (inputNode == null)
                    return new BookPriceResult();

                var products = inputNode.SelectNodes(".//div[contains(@class, 'item-product')]");
                if (products == null || products.Count == 0)
                    return new BookPriceResult();

                var possibleBooks = GetReturnedBooksByTitle(products, parameters.BookTitle);
                var result = ChooseBestBookOption(possibleBooks, parameters.BookTitle, parameters.IsExactSearch);

                return new BookPriceResult
                {
                    Price = result.Price,
                    Title = result.Title,
                    Author = result.Author,
                    Website = website
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"erro ao consultar {parameters.Source.Url}");
                return new BookPriceResult();
            }
        }

        private List<BookPriceResult> GetReturnedBooksByTitle(HtmlNodeCollection products, string bookTitle)
        {
            var possibleBooks = new List<BookPriceResult>();
            foreach (var product in products)
            {
                var childnode = product.ChildNodes;

                try
                {
                    var authorNode = childnode[7].InnerText.Trim();
                    var titleNode = childnode[9].InnerText.Trim();
                    var discountNode = childnode[11].InnerText.Trim();
                    var priceNodes = childnode[13].ChildNodes;

                    var oldPrice = Convert.ToDecimal(priceNodes[1].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                    var newPrice = Convert.ToDecimal(priceNodes[4].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                    int discount = (int)Math.Abs(newPrice * 100 / oldPrice) - 100;

                    var book = new BookPriceResult
                    {
                        Title = titleNode,
                        Author = authorNode,
                        Price = newPrice,
                        Discount = discount
                    };
                    possibleBooks.Add(book);
                }
                catch
                {
                    // ignora produtos que não tenham estrutura esperada
                }
            }

            return possibleBooks;
        }

        private BookPriceResult ChooseBestBookOption(List<BookPriceResult> possibleBooks, string bookTitle, bool isExactSearch)
        {
            bookTitle = bookTitle.ToUpper().Trim();
            if (possibleBooks.Count == 0)
                return new BookPriceResult();

            if (isExactSearch)
                return possibleBooks.FirstOrDefault(b => b.Title.ToUpper() == bookTitle) ?? new BookPriceResult();

            var bestPrice = possibleBooks.Min(b => b.Price);
            var bestBook = possibleBooks.FirstOrDefault(b => b.Price == bestPrice);
            return bestBook ?? new BookPriceResult();
        }

    }
}
