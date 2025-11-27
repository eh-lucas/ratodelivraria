using System.Diagnostics;
using API.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.Core.Scrapers.Cedet.SingleSearch;

/// <summary>
/// Scraper usando Selenium (mais lento, usar apenas se necessário)
/// </summary>
public class CedetSingleSearch : IScraper
{
    public ScraperTypeEnum ScraperType => ScraperTypeEnum.CedetSingleSearch;

    private const string GridXPath = "//*[@id=\"column-right\"]/div[5]";
    private const string SearchBoxXPath = "//*[@id=\"input-search\"]";
    private const string SearchButtonXPath = "//*[@id=\"doSearch\"]";

    public async Task<QueryResult> ExecuteSearch(SearchParameter parameters)
    {
        var provider = parameters.Source ?? new Provider { Id = 0, Name = "Unknown", Url = string.Empty };
        var stopwatch = Stopwatch.StartNew();
        IWebDriver? driver = null;

        try
        {
            driver = InitiateWebDriver();
            var searchTerm = parameters.Isbn;

            SearchBookInBox(driver, provider.Url, searchTerm);

            var grid = driver.FindElement(By.XPath(GridXPath));
            var book = CreateBook(grid, searchTerm);

            stopwatch.Stop();
            driver.Quit();

            if (book != null && !string.IsNullOrEmpty(book.Title) && book.Price > 0)
            {
                return QueryResult.CreateSuccess(
                    provider,
                    book.Title,
                    book.Author,
                    book.Price,
                    book.Discount,
                    stopwatch.ElapsedMilliseconds);
            }

            return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            driver?.Quit();

            Console.WriteLine($"erro ao consultar no site {provider.Url}");

            return QueryResult.CreateFailure(
                provider,
                QueryErrorType.Unknown,
                ex.Message,
                stopwatch.ElapsedMilliseconds);
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

    private ParsedBook? CreateBook(IWebElement grid, string bookTitle)
    {
        IReadOnlyList<IWebElement> itemProductElement = grid.FindElements(By.ClassName("item-product"));
        foreach (var element in itemProductElement)
        {
            var elementName = element.FindElement(By.ClassName("name"));
            if (elementName.Text.Equals(bookTitle))
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
                    discount = 0;
                }

                return new ParsedBook
                {
                    Title = bookTitle,
                    Author = author,
                    Price = priceNew,
                    Discount = discount
                };
            }
        }

        return null;
    }

    private IWebDriver InitiateWebDriver()
    {
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        IWebDriver driver = new ChromeDriver();
        return driver;
    }

    private class ParsedBook
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Discount { get; set; }
    }
}
