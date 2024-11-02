using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using API.Utils;
using HtmlAgilityPack;
using OpenQA.Selenium;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.SearchTypes.Cedet
{
    public class CedetSingleAgility
    {
        private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";
        private const string SearchBoxXPath = "//*[@id=\"input-search\"]";
        private const string SearchButtonXPath = "//*[@id=\"doSearch\"]";

        public Book Start()
        {
            var url = "https://livraria.seminariodefilosofia.org/index.php?route=product/search&search=O%20idiota";
            var web = new HtmlWeb();
            var doc = web.Load(url);
            var inputNode = doc.DocumentNode.SelectSingleNode(GridXPath);
            var products = inputNode.SelectNodes("//div[contains(@class, 'item-product')]");
            if (products != null)
            {
                CreateBook(products, "O idiota");

            }

            return new Book();
        }

        public Book? CreateBook(HtmlNodeCollection products, string bookTitle)
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

            //    }


            //    var priceNode = product.SelectSingleNode(".//div[contains(@class, 'price-new')]");
            //    var price = priceNode?.InnerText.Trim();

            //    Console.WriteLine($"Preço: {price}");
            return new Book();

            //return new Book();
        }
        //private string[]    
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