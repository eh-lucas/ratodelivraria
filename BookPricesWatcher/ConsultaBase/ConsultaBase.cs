using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.Support.UI;

namespace BookPricesWatcher.ConsultaBase;
class ConsultaBase
{

    public async void Execute()
    {

        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        IWebDriver driver = new ChromeDriver();

        string url = "https://livraria.seminariodefilosofia.org/";
        //  https://livraria.seminariodefilosofia.org/cartas-de-um-diabo-a-seu-aprendiz 
        string bookTitle = "Cartas de um diabo a seu aprendiz";
        driver.Navigate().GoToUrl(url);

        IWebElement searchBox = driver.FindElement(By.XPath("//*[@id=\"input-search\"]"));
        if (searchBox.Displayed) 
            searchBox.Click();

        searchBox.SendKeys(bookTitle);

        var searchButtonElem = driver.FindElement(By.XPath("//*[@id=\"doSearch\"]"));
        if (searchButtonElem.Displayed) 
            searchButtonElem.Click();

        IWebElement grid = driver.FindElement(By.XPath(""))
    }
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
