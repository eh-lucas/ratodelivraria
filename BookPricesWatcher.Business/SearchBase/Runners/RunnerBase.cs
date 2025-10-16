using OpenQA.Selenium;
using Sherlock.Business.Interfaces;
using Sherlock.Business.SearchBase.Runners.Cedet;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.SearchBase.Runners;
// essa classe deve ser o mais generica possivel para uma busca
// deve receber um objeto parametro e somente com isso deve ser capaz de executar a busca
// deve 1 fazer a chamada, 2 tratar o dado, 3 devolver o resultado
public abstract class RunnerBase<TParam> : IDataSource
{
    public abstract SearchTypeEnum SearchType { get; }

    //public async Task<List<Book>> ExecuteSearch(Requestor requestor, ConsultaBase consulta)
    public abstract Task<CedetSingleSearchResult> ExecuteSearch(TParam parameters);
    //public abstract Task<TResult> TreatReturnedData(TResult result);

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
