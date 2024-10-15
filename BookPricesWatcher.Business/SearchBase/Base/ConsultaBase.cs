using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.Base;
public abstract class ConsultaBase
{
    public async Task<List<Book>> Execute(Requestor requestor)
    {
        List<Book> books = new List<Book>();
        var websites = Websites.CedetSites;

        foreach (string website in websites)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            IWebDriver driver = new ChromeDriver();

            var bookTitle = requestor.InputParameters.BookTitle;
            driver.Navigate().GoToUrl(website);

            SearchBookInBox(driver, bookTitle);

            try
            {
                IWebElement grid = driver.FindElement(By.XPath("//*[@id=\"column-right\"]/div[5]"));
                Book book = CreateBook(grid, bookTitle);
                book.WebSite = website;
                books.Add(book);
                driver.Quit();
            }
            catch
            {
                Console.WriteLine($"erro ao consultar no site {website}");
            }
        }

        var minimalPrice = 0.0;
        var resultWebsite = "";
        foreach (var book in books)
        {
            if (minimalPrice == 0.0)
                minimalPrice = book.Price;

            if (book.Price >= minimalPrice)
                continue;
            else
                minimalPrice = book.Price;
        }

        List<Book> resultBooks = new List<Book>();
        foreach (var book in books)
        {
            if (book.Price == minimalPrice)
                resultBooks.Add(book);
        }

        return resultBooks;
    }
    public abstract void SearchBookInBox(IWebDriver driver, string bookTitle);
    public abstract Book? CreateBook(IWebElement grid, string bookTitle);
    private async Task LoadPageAsync(IWebDriver driver, string url)
    {
        // Navegar para a URL sem Task.Run
        driver.Navigate().GoToUrl(url);

        // Continuar aguardando o carregamento da página de forma assíncrona
        while (true)
        {
            await Task.Delay(500); // Pausa para evitar loop de verificação muito rápida
            if ((driver as IJavaScriptExecutor).ExecuteScript("return document.readyState").Equals("complete"))
                break;
        }
    }

}
