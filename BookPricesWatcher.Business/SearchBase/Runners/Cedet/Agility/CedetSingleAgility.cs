using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using API.Utils;
using HtmlAgilityPack;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.SearchBase.Runners.Cedet.Agility
{
    public class CedetSingleAgility : RunnerBase<CedetSingleSearchParams>
    {
        public override SearchTypeEnum SearchType => SearchTypeEnum.CedetSingleAgility;

        private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";
        private const string SearchBoxXPath = "//*[@id=\"input-search\"]";
        private const string SearchButtonXPath = "//*[@id=\"doSearch\"]";

        public List<Book?> GetReturnedBooksByTitle(HtmlNodeCollection products, string bookTitle)
        {
            var possibleBooks = new List<Book>();
            foreach (var product in products)
            {
                var childnode = product.ChildNodes;
                var authorNode = childnode[7].InnerText.Trim();
                var titleNode = childnode[9].InnerText.Trim();
                var discountNode = childnode[11].InnerText.Trim();
                var childnodes = childnode[13].ChildNodes;
                var oldPrice = Convert.ToDouble(childnodes[1].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                var newPrice = Convert.ToDouble(childnodes[4].InnerText.CleanPrice(), CultureInfo.InvariantCulture);
                var discount = (int)Math.Abs(newPrice * 100 / oldPrice) - 100;

                Book book = new Book(titleNode, authorNode, newPrice, discount, null);
                possibleBooks.Add(book);
            }

            return possibleBooks;
        }

        public async override Task<CedetSingleSearchResult> ExecuteSearch(CedetSingleSearchParams parameters)
        {
            var url = "https://livraria.seminariodefilosofia.org/index.php?route=product/search&search=O%20idiota";
            var web = new HtmlWeb();
            var doc = web.Load(url);
            var inputNode = doc.DocumentNode.SelectSingleNode(GridXPath);
            var products = inputNode.SelectNodes("//div[contains(@class, 'item-product')]");

            if (products != null)
            {
                //var result = GetReturnedBooksByTitle(products, parameters.BookTitle);
                var possibleBooks = GetReturnedBooksByTitle(products, "O idiota");

                var result = ChooseBestBookOption(possibleBooks, parameters.BookTitle, parameters.IsExactSearch);
               
                return new CedetSingleSearchResult() { Book = result};
            }

            return new CedetSingleSearchResult();
        }

        private Book ChooseBestBookOption(List<Book?> possibleBooks, string bookTitle, bool isExactSearch)
        {
            bookTitle = bookTitle.ToUpper().Trim();
            if (possibleBooks.Count < 1)
                throw new NotImplementedException();

            if (isExactSearch)
                return possibleBooks.FirstOrDefault(b => b.Title.ToUpper() == bookTitle);

            var bestPrice = possibleBooks.Min(b => b.Price);
            var bestBook = possibleBooks.Find(b => b.Price == bestPrice);

            if (bestBook is List<Book>)
                throw new NetworkInformationException();
            if (bestBook is null)
                throw new NetworkInformationException();

            return new Book();
        }
    }
}
//{

//    string author = element.FindElement(By.ClassName("author")).Text;
//    double priceNew = Convert.ToDouble(element.FindElement(By.ClassName("price-new")).Text);
//    int discount;
//    try
//    {
//        var auxText = element.FindElement(By.ClassName("price-old")).Text;
//        auxText = auxText.CleanPrice();
//        var oldPrice = Convert.ToDouble(auxText);
//        discount = (int)Math.Abs(priceNew * 100 / oldPrice) - 100;
//    }
//    catch
//    {
//        double oldPrice = priceNew;
//        discount = 0;
//    }

//    return new Book(bookTitle, author, priceNew, discount, "");
//}


//if (books.Count > 1)
//{
//    var minPrice = books.Min(book => book.Price);
//    var cheapestBook = books.Find(book => book.Price == minPrice);
//    return cheapestBook;
//}