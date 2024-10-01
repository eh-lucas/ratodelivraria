using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookPricesWatcher.ConsultaBase;
using BookPricesWatcher.Model;
using BookPricesWatcher.Utils;
using OpenQA.Selenium;

namespace BookPricesWatcher.SearchTypes
{
    public class CedetSingleSearch : ConsultaBase.ConsultaBase
    {
        private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";
        private const string SearchBoxXPath = "//*[@id=\"input-search\"]";
        private const string SearchButtonXPath = "//*[@id=\"doSearch\"]";

        public override void SearchBookInBox(IWebDriver driver, string bookTitle)
        {
            IWebElement searchBox = driver.FindElement(By.XPath(SearchBoxXPath));
            if (searchBox.Displayed)
                searchBox.Click();

            searchBox.SendKeys(bookTitle);

            var searchButtonElem = driver.FindElement(By.XPath(SearchButtonXPath));
            if (searchButtonElem.Displayed)
                searchButtonElem.Click();
        }

        public override Book? CreateBook(IWebElement grid, string bookTitle)
        {
            Book book = null;
            IReadOnlyList<IWebElement> itemProductElement = grid.FindElements(By.ClassName("item-product"));
            foreach (var element in itemProductElement)
            {
                var elementName = element.FindElement(By.ClassName("name"));
                if (elementName.Text.Contains(bookTitle))
                {

                    string author = element.FindElement(By.ClassName("author")).Text;
                    double priceNew = Convert.ToDouble(element.FindElement(By.ClassName("price-new")).Text);
                    int discount;
                    try
                    {
                        var auxText = element.FindElement(By.ClassName("price-old")).Text;
                        auxText = auxText.CleanPrice();
                        var oldPrice = Convert.ToDouble(auxText);
                        discount = (int)Math.Abs((priceNew * 100) / oldPrice) - 100;
                    }
                    catch
                    {
                        double oldPrice = priceNew;
                        discount = 0;
                    }

                    book = new Book(bookTitle, author, priceNew, discount);
                }
                else continue;
            }
            return book;
        }
    }
}
