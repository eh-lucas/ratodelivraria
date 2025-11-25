using System.Diagnostics;
using System.Globalization;
using API.Utils;
using HtmlAgilityPack;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.Core.Scrapers.Cedet.Agility;

public class CedetSingleAgility : IScraper
{
    public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetSingleAgility;

    private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";

    public async Task<QueryResult> ExecuteSearch(SearchParameter parameters)
    {
        var provider = parameters.Source ?? new Provider { Id = 0, Name = "Unknown", Url = string.Empty };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var bookTitle = parameters.BookTitle;

            var web = new HtmlWeb();
            var doc = web.Load(provider.Url);

            stopwatch.Stop();

            var inputNode = doc.DocumentNode.SelectSingleNode(GridXPath);
            var products = inputNode?.SelectNodes("//div[contains(@class, 'item-product')]");

            if (products == null || products.Count == 0)
            {
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            var possibleBooks = GetReturnedBooksByTitle(products, bookTitle);
            var result = ChooseBestBookOption(possibleBooks, bookTitle, parameters.IsExactSearch);

            if (result != null && !string.IsNullOrEmpty(result.Title) && result.Price > 0)
            {
                return QueryResult.CreateSuccess(
                    provider,
                    result.Title,
                    result.Author,
                    result.Price,
                    result.Discount,
                    stopwatch.ElapsedMilliseconds);
            }

            return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return QueryResult.CreateFailure(
                provider,
                QueryErrorType.Unknown,
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private List<ParsedBook> GetReturnedBooksByTitle(HtmlNodeCollection products, string bookTitle)
    {
        var possibleBooks = new List<ParsedBook>();

        foreach (var product in products)
        {
            try
            {
                var childnode = product.ChildNodes;
                var authorNode = childnode[7].InnerText.Trim();
                var titleNode = childnode[9].InnerText.Trim();
                var discountNode = childnode[11].InnerText.Trim();
                var childnodes = childnode[13].ChildNodes;
                var oldPrice = Convert.ToDecimal(childnodes[1].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                var newPrice = Convert.ToDecimal(childnodes[4].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                var discount = (int)Math.Abs(newPrice * 100 / oldPrice) - 100;

                possibleBooks.Add(new ParsedBook
                {
                    Title = titleNode,
                    Author = authorNode,
                    Price = newPrice,
                    Discount = discount
                });
            }
            catch
            {
                // Ignora erros de parsing
            }
        }

        return possibleBooks;
    }

    private ParsedBook? ChooseBestBookOption(List<ParsedBook> possibleBooks, string bookTitle, bool isExactSearch)
    {
        if (possibleBooks.Count == 0)
            return null;

        bookTitle = bookTitle.ToUpper().Trim();

        if (isExactSearch)
        {
            return possibleBooks.FirstOrDefault(b => b.Title.ToUpper() == bookTitle);
        }

        var bestPrice = possibleBooks.Min(b => b.Price);
        return possibleBooks.FirstOrDefault(b => b.Price == bestPrice);
    }

    private class ParsedBook
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Discount { get; set; }
    }
}
