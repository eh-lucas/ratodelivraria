using System.Diagnostics;
using Sherlock.Business.Core.Scrapers;

namespace Sherlock.Business.Core.Base;

// essa classe deve ser genérica.
// deve processar uma transação inteiramente, a partir de um objeto Requestor, contendo todas as configurações
// de processamento que o usuario definiu.
// Ela fará:
// - 1. Montar req: organizar a chamada da consulta (seja ela unica ou multipla)
// - 2. Cache: verificar se os resultados ja existem em banco 
// - 3. Custo: calcular custo da transacao
// - 4. Registrar: atualizar registros no banco

public class W16Engine
{
    private readonly Comparator _comparator;
    private ScraperFactory _scraperFactory;

    public W16Engine()
    {
        _comparator = new Comparator();
        _scraperFactory = new ScraperFactory();
    }

    public async Task<SearchResult> ExecuteTransaction(Requestor requestor)
    {
        var stopwatch = Stopwatch.StartNew(); // inicia o cronômetro

        var preResults = new List<SearchResult>();

        Console.WriteLine("Verifica resultados cacheados");

        Console.WriteLine("Se os dados vindos do cache nao supriram tudo, prepara para rodar os runners.");

        // factory para criar instancias do RunnerBase
        Console.WriteLine("Cria instancias do RunnerBase");
        var runners = _scraperFactory.CreateScrapers(requestor);

        foreach (var runner in runners)
        {
            foreach (var source in requestor.SourcesToSearch)
            {
                var parameters = requestor.SearchParameters;
                parameters.Source = source;

                var singleResult = await runner.ExecuteSearch(parameters);
                preResults.Add(singleResult);
            }
        }

        Console.WriteLine("Roda o(s) Runner(s) consultando todas as fontes solicitadas");

        return _comparator.Compare(preResults);


        Console.WriteLine("Salva resultados das consultas no banco, se configurado para tal");

        Console.WriteLine("Calcula custo da transação e desconta saldo do cliente");

        Console.WriteLine("");

        stopwatch.Stop(); // para o cronômetro mesmo se ocorrer exceção
        Console.WriteLine($"⏱ Tempo total de execução: {stopwatch.ElapsedMilliseconds} ms");
    }
}
