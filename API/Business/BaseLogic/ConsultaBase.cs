using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Domain;
using API.Model;
using OpenQA.Selenium.DevTools.V127.Page;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace API.Business.BaseLogic;
public abstract class ConsultaBase
{
    public async Task<List<Book>> Execute(Requestor requestor)
    {
        List<Book> books = new List<Book>();
        var websites = Websites.CedetSites;

        List<Task> tasks = new List<Task>();
        foreach (string website in websites)
        {
            Task task = Task.Run(async () =>
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--start-maximized");
                IWebDriver driver = new ChromeDriver();

                var bookTitle = requestor.InputParameters.Titles[0];
                driver.Navigate().GoToUrl(website);

                SearchBookInBox(driver, bookTitle);

                try
                {
                    IWebElement grid = driver.FindElement(By.XPath("//*[@id=\"column-right\"]/div[5]"));
                    Book book = CreateBook(grid, bookTitle);
                    book.WebSite = website;
                    books.Add(book);
                }
                catch
                {
                    Console.WriteLine($"erro ao consultar no site {website}");
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll();

        var minimalPrice = 0.0;
        var resultWebsite = "";
        foreach (var book in books)
        {
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
