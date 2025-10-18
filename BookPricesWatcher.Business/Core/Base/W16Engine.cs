using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;
using System.Diagnostics;

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

        var preResults = new List<BookPriceResult>();
        SearchResult result = new()
        {
            InicioConsulta = DateTime.Now
        };

        Console.WriteLine("Verifica resultados cacheados");

        Console.WriteLine("Se os dados vindos do cache nao supriram tudo, prepara para rodar os runners.");

        Console.WriteLine("Cria instancias de scrapers");
        var scrapers = _scraperFactory.CreateScrapers(requestor);

        Console.WriteLine("Roda o(s) scraper(s) consultando todas as fontes solicitadas");
        foreach (var scraper in scrapers)
        {
            foreach (var source in requestor.SourcesToSearch)
            {
                var parameters = requestor.SearchParameters;
                parameters.Source = source;

                var singleResult = await scraper.ExecuteSearch(parameters);
                preResults.Add(singleResult);
            }
        }


        Console.WriteLine("Retorna o melhor preço");
        result.BookPriceResult = _comparator.Compare(preResults);


        Console.WriteLine("Salva resultados das consultas no banco, se configurado para tal");


        Console.WriteLine("Calcula custo da transação e desconta saldo do cliente");
        result.CustoCreditos = 10;

        
        Console.WriteLine("");


        stopwatch.Stop(); // para o cronômetro mesmo se ocorrer exceção
        Console.WriteLine($"⏱ Tempo total de execução: {stopwatch.ElapsedMilliseconds} ms");
        result.TempoDecorrido = stopwatch.ElapsedMilliseconds;
        result.FimConsulta = DateTime.Now;

        return result;
    }
}
