using API.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.SearchBase.Runners.Cedet
{
    // etapas que tem numa busca
    // 1 fazer a busca no site
    // 2 coletar todos os resultados
    // 3 interpretar dados
    public class CedetSingleSearch : RunnerBase<CedetSingleSearchParams>
    {
        public override SearchTypeEnum SearchType => SearchTypeEnum.CedetSingleSearch;

        private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";
        private const string SearchBoxXPath = "//*[@id=\"input-search\"]";
        private const string SearchButtonXPath = "//*[@id=\"doSearch\"]";

        public async override Task<CedetSingleSearchResult> ExecuteSearch(CedetSingleSearchParams parameters)
        {
            List<Book> books = new List<Book>();

            var driver = InitiateWebDriver();
            var website = GetWebsiteToSearch(parameters);
            var bookTitle = GetBookToSearch(parameters);

            SearchBookInBox(driver, website, bookTitle);

            Book book = null;
            try
            {
                IWebElement grid = driver.FindElement(By.XPath("//*[@id=\"column-right\"]/div[5]"));

                book = CreateBook(grid, bookTitle);
                book.WebSite = website;
                books.Add(book);
                driver.Quit();
            }
            catch
            {
                Console.WriteLine($"erro ao consultar no site {website}");
            }

            return new CedetSingleSearchResult() { Book = book};
        }

        //public override Task<CedetSingleSearchResult> TreatReturnedData(CedetSingleSearchResult result)
        //{
        //    throw new NotImplementedException();
        //}

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

        private Book? CreateBook(IWebElement grid, string bookTitle)
        {
            IReadOnlyList<IWebElement> itemProductElement = grid.FindElements(By.ClassName("item-product"));
            foreach (var element in itemProductElement)
            {
                var elementName = element.FindElement(By.ClassName("name"));
                if (elementName.Text.Equals(bookTitle)) // Equals porque sempre há apenas um livro com o mesmo nome nos sites da Cedet.
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

                    return new Book(bookTitle, author, priceNew, discount, "");
                }
            }

            //if (books.Count > 1)
            //{
            //    var minPrice = books.Min(book => book.Price);
            //    var cheapestBook = books.Find(book => book.Price == minPrice);
            //    return cheapestBook;
            //}
            return null;
        }

        private string GetWebsiteToSearch(CedetSingleSearchParams parameters)
        {
            return parameters.Website;
        }

        private string GetBookToSearch(CedetSingleSearchParams parameters)
        {
            return parameters.BookTitle;
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
