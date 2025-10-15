using HtmlAgilityPack;
using Sherlock.Business.SearchBase.Base;
using Sherlock.Domain.Entities;
using System.Globalization;
using API.Utils;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.SearchBase.SearchTypes.Cedet.HttpClient
{
    public class CedetSingleSearchHttpClient : ConsultaBase<CedetSingleSearchParams, CedetSingleSearchResult>
    {

        private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";

        public override SearchTypeEnum SearchType => SearchTypeEnum.CedetSingleAgilityHttpClient;
        public async override Task<CedetSingleSearchResult> ExecuteSearch(CedetSingleSearchParams parameters)
        {
            string searchTerm = Uri.EscapeDataString(parameters.BookTitle);
            string url = $"https://livraria.seminariodefilosofia.org/index.php?route=product/search&search={searchTerm}";

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
                return new CedetSingleSearchResult();

            var products = inputNode.SelectNodes(".//div[contains(@class, 'item-product')]");
            if (products == null || products.Count == 0)
                return new CedetSingleSearchResult();

            var possibleBooks = GetReturnedBooksByTitle(products, parameters.BookTitle);
            var result = ChooseBestBookOption(possibleBooks, parameters.BookTitle, parameters.IsExactSearch);

            return new CedetSingleSearchResult() { Book = result };
        }

        private List<Book> GetReturnedBooksByTitle(HtmlNodeCollection products, string bookTitle)
        {
            var possibleBooks = new List<Book>();
            foreach (var product in products)
            {
                var childnode = product.ChildNodes;

                try
                {
                    var authorNode = childnode[7].InnerText.Trim();
                    var titleNode = childnode[9].InnerText.Trim();
                    var discountNode = childnode[11].InnerText.Trim();
                    var priceNodes = childnode[13].ChildNodes;

                    double oldPrice = Convert.ToDouble(priceNodes[1].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                    double newPrice = Convert.ToDouble(priceNodes[4].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                    int discount = (int)Math.Abs(newPrice * 100 / oldPrice) - 100;

                    var book = new Book(titleNode, authorNode, newPrice, discount, null);
                    possibleBooks.Add(book);
                }
                catch
                {
                    // ignora produtos que não tenham estrutura esperada
                }
            }

            return possibleBooks;
        }

        private Book ChooseBestBookOption(List<Book> possibleBooks, string bookTitle, bool isExactSearch)
        {
            bookTitle = bookTitle.ToUpper().Trim();
            if (possibleBooks.Count == 0)
                return new Book();

            if (isExactSearch)
                return possibleBooks.FirstOrDefault(b => b.Title.ToUpper() == bookTitle) ?? new Book();

            var bestPrice = possibleBooks.Min(b => b.Price);
            var bestBook = possibleBooks.FirstOrDefault(b => b.Price == bestPrice);
            return bestBook ?? new Book();
        }

    }
}
