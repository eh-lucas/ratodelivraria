using API.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.Core.Scrapers.Cedet.SingleSearch
{
    // etapas que tem numa busca
    // 1 fazer a busca no site
    // 2 coletar todos os resultados
    // 3 interpretar dados
    public class CedetSingleSearch : IScraper
    {
        public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetSingleSearch;

        private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";
        private const string SearchBoxXPath = "//*[@id=\"input-search\"]";
        private const string SearchButtonXPath = "//*[@id=\"doSearch\"]";

        public async Task<BookPriceResult> ExecuteSearch(SearchParameter parameters)
        {
            var driver = InitiateWebDriver();
            var website = parameters.Source.Url;
            var bookTitle = parameters.BookTitle;

            SearchBookInBox(driver, website, bookTitle);

            try
            {
                var grid = driver.FindElement(By.XPath(GridXPath));

                var book = CreateBook(grid, bookTitle);

                driver.Quit();

                return new BookPriceResult
                {
                    Price = book.Price,
                    Title = book.Title,
                    Author = book.Author,
                    Website = website
                };
            }
            catch
            {
                Console.WriteLine($"erro ao consultar no site {website}");
                return new BookPriceResult();
            }
        }

        private void SearchBookInBox(IWebDriver driver, string website, string bookTitle)
        {
            driver.Navigate().GoToUrl(website);

            IWebElement searchBox = driver.FindElement(By.XPath(SearchBoxXPath));
            if (searchBox.Displayed)
                searchBox.Click();

            searchBox.SendKeys(bookTitle);

            var searchButtonElem = driver.FindElement(By.XPath(SearchButtonXPath));
            if (searchButtonElem.Displayed)
                searchButtonElem.Click();
        }

        private BookPriceResult CreateBook(IWebElement grid, string bookTitle)
        {
            IReadOnlyList<IWebElement> itemProductElement = grid.FindElements(By.ClassName("item-product"));
            foreach (var element in itemProductElement)
            {
                var elementName = element.FindElement(By.ClassName("name"));
                if (elementName.Text.Equals(bookTitle)) // Equals porque sempre há apenas um livro com o mesmo nome nos sites da Cedet.
                {

                    var author = element.FindElement(By.ClassName("author")).Text;
                    var priceNew = Convert.ToDecimal(element.FindElement(By.ClassName("price-new")).Text);
                    int discount;
                    try
                    {
                        var auxText = element.FindElement(By.ClassName("price-old")).Text;
                        auxText = auxText.CleanPrice();
                        var oldPrice = Convert.ToDecimal(auxText);
                        discount = (int)Math.Abs(priceNew * 100 / oldPrice) - 100;
                    }
                    catch
                    {
                        var oldPrice = priceNew;
                        discount = 0;
                    }

                    return new BookPriceResult
                    {
                        Title = bookTitle,
                        Author = author,
                        Price = priceNew,
                        Discount = discount
                    };
                }
            }

            return new BookPriceResult();
        }

        private IWebDriver InitiateWebDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            IWebDriver driver = new ChromeDriver();
            return driver;
        }
    }
}
