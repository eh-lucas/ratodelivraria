using API.Business.BaseLogic;
using API.Domain;
using API.Utils;
using OpenQA.Selenium;

namespace API.Business.SearchTypes
{
    public class CedetSingleSearch : ConsultaBase
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
            List<Book?> books = new List<Book?>();
            IReadOnlyList<IWebElement> itemProductElement = grid.FindElements(By.ClassName("item-product"));
            foreach (var element in itemProductElement)
            {
                var elementName = element.FindElement(By.ClassName("name"));
                if (elementName.Text.Equals(bookTitle))
                {

                    string author = element.FindElement(By.ClassName("author")).Text;
                    double priceNew = Convert.ToDouble(element.FindElement(By.ClassName("price-new")).Text);
                    int discount;
                    try
                    {
                        var auxText = element.FindElement(By.ClassName("price-old")).Text;
                        auxText = auxText.CleanPrice();
                        var oldPrice = Convert.ToDouble(auxText);
                        discount = (int)Math.Abs(priceNew * 100 / oldPrice) - 100;
                    }
                    catch
                    {
                        double oldPrice = priceNew;
                        discount = 0;
                    }

                    books.Add(new Book(bookTitle, author, priceNew, discount, ""));
                }
                else continue;
            }

            if (books.Count > 1)
            {
                var minPrice = books.Min(book => book.Price);
                var cheapestBook = books.Find(book => book.Price == minPrice);
                return cheapestBook;
            }

            return books[0];
        }
    }
}
