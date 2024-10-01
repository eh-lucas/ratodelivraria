using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookPricesWatcher.Model;
using BookPricesWatcher.Utils;
using OpenQA.Selenium.DevTools.V127.Page;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace BookPricesWatcher.ConsultaBase;
public abstract class ConsultaBase
{
    

    public async void Execute(Requestor requestor)
    {
        List<Book> books = new List<Book>();
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        IWebDriver driver = new ChromeDriver();

        string url = requestor.Websites[1];
        string bookTitle = requestor.Books[1].Title;

        driver.Navigate().GoToUrl(url);

        SearchBookInBox(driver, bookTitle);

        IWebElement grid = driver.FindElement(By.XPath("//*[@id=\"column-right\"]/div[5]"));

        Book book = CreateBook(grid, bookTitle);

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
