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
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        // HttpClient estático para reutilização de conexões
        private static readonly System.Net.Http.HttpClient _httpClient;

        static CedetSingleSearchHttpClient()
        {
            _httpClient = new System.Net.Http.HttpClient
            {
                Timeout = RequestTimeout
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetSingleAgilityHttpClient;

        public async Task<BookPriceResult> ExecuteSearch(SearchParameter parameters)
        {
            var website = parameters.Source?.Url ?? string.Empty;

            try
            {
                if (string.IsNullOrEmpty(parameters.BookTitle))
                    return new BookPriceResult();

                string searchTerm = Uri.EscapeDataString(parameters.BookTitle);
                string url = $"{website}index.php?route=product/search&search={searchTerm}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new BookPriceResult();

                var html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var inputNode = doc.DocumentNode.SelectSingleNode(GridXPath);
                if (inputNode == null)
                    return new BookPriceResult();

                var products = inputNode.SelectNodes(".//div[contains(@class, 'item-product')]");
                if (products == null || products.Count == 0)
                    return new BookPriceResult();

                var possibleBooks = GetReturnedBooksByTitle(products, website);
                var result = ChooseBestBookOption(possibleBooks, parameters.BookTitle, parameters.IsExactSearch);

                return result;
            }
            catch (TaskCanceledException)
            {
                // Timeout - não propaga exceção, retorna vazio
                return new BookPriceResult();
            }
            catch (HttpRequestException)
            {
                // Erro de rede - não propaga exceção, retorna vazio
                return new BookPriceResult();
            }
        }

        private List<BookPriceResult> GetReturnedBooksByTitle(HtmlNodeCollection products, string website)
        {
            var possibleBooks = new List<BookPriceResult>();

            foreach (var product in products)
            {
                try
                {
                    var childnode = product.ChildNodes;
                    if (childnode.Count < 14)
                        continue;

                    var authorNode = childnode[7].InnerText.Trim();
                    var titleNode = childnode[9].InnerText.Trim();
                    var priceNodes = childnode[13].ChildNodes;

                    if (priceNodes.Count < 5)
                        continue;

                    var oldPrice = Convert.ToDecimal(priceNodes[1].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                    var newPrice = Convert.ToDecimal(priceNodes[4].InnerText.CleanPrice(), CultureInfo.InvariantCulture);

                    // Cálculo correto do desconto: percentual de economia
                    int discount = oldPrice > 0
                        ? (int)Math.Round(100 * (1 - (newPrice / oldPrice)))
                        : 0;

                    var book = new BookPriceResult
                    {
                        Title = titleNode,
                        Author = authorNode,
                        Price = newPrice,
                        Discount = discount,
                        Website = website
                    };
                    possibleBooks.Add(book);
                }
                catch
                {
                    // Ignora produtos com estrutura HTML inesperada
                }
            }

            return possibleBooks;
        }

        private static BookPriceResult ChooseBestBookOption(List<BookPriceResult> possibleBooks, string bookTitle, bool isExactSearch)
        {
            if (possibleBooks.Count == 0)
                return new BookPriceResult();

            bookTitle = bookTitle.ToUpper().Trim();

            if (isExactSearch)
            {
                var exactMatch = possibleBooks.FirstOrDefault(b =>
                    !string.IsNullOrEmpty(b.Title) && b.Title.ToUpper().Trim() == bookTitle);

                return exactMatch ?? new BookPriceResult();
            }

            // Retorna o livro com menor preço (maior que zero)
            return possibleBooks
                .Where(b => b.Price > 0)
                .OrderBy(b => b.Price)
                .FirstOrDefault() ?? new BookPriceResult();
        }
    }
}
