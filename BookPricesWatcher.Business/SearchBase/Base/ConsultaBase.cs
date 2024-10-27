using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.Base;
// essa classe deve ser o mais generica possivel para uma busca
// deve receber um objeto parametro e somente com isso deve ser capaz de executar a busca
// deve 1 fazer a chamada, 2 tratar o dado, 3 devolver o resultado
public abstract class ConsultaBase<TParam, TResult>
    where TParam : SearchParameters
    where TResult : SearchResult
{
    //public async Task<List<Book>> ExecuteMainLoop(Requestor requestor, ConsultaBase consulta)
    public async Task<SearchResult> ExecuteMainLoop(TParam parameters)
    {
        List<Book> books = new List<Book>();
        //var dataSource = requestor.SearchTypeId;

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

        var minimalPrice = 0.0;
        var resultWebsite = "";


        if (book.Price >= minimalPrice)
            minimalPrice = book.Price;
        else
            minimalPrice = book.Price;

        var resultBooks = new CedetSingleSearchResult(book);

        return resultBooks;
    }

    private IWebDriver InitiateWebDriver()
    {
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        IWebDriver driver = new ChromeDriver();
        return driver;
    }
    public abstract void SearchBookInBox(IWebDriver driver, string website, string bookTitle);
    public abstract Book? CreateBook(IWebElement grid, string bookTitle);
    public abstract string GetWebsiteToSearch(TParam parameters);
    public abstract string GetBookToSearch(TParam parameters);
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
