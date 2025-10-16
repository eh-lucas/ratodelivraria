using OpenQA.Selenium.BiDi.Modules.Script;
using Sherlock.Business.SearchBase.Runners;
using Sherlock.Business.SearchBase.Runners.Cedet;
using Sherlock.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Sherlock.Business.SearchBase.Base;
// essa classe deve:
// - organizar a chamada da consulta (seja ela unica ou conjugada)
// - verificar se os resultados ja existem em banco 
// - calcular custo da transacao
// - atualizar registros no banco
public class Engine
{
    private readonly Comparator _comparator;

    public Engine()
    {
        _comparator = new Comparator();
    }

    public async Task<SearchResult> ExecuteTransaction<TConsulta, TParam>(Requestor requestor)
        where TConsulta : RunnerBase<TParam>, new()
        where TParam : SearchParameters
    {
        var consulta = new TConsulta();
        var preResults = new List<CedetSingleSearchResult>();
        var semaphore = new SemaphoreSlim(5); // máximo de 5 tarefas simultâneas
        var tasks = new List<Task<CedetSingleSearchResult>>();

        foreach (var source in requestor.SourcesToSearch)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var parameters = requestor.SearchParameters;
                    parameters.Source = source;

                    var singleResult = await consulta.ExecuteSearch((TParam)parameters);
                    return singleResult;
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        // aguarda todas as buscas terminarem
        var results = await Task.WhenAll(tasks);

        // adiciona os resultados coletados
        preResults.AddRange(results);

        return _comparator.Compare(preResults);
    }
}
