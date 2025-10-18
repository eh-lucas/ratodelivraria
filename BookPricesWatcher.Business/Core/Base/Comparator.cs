using Sherlock.Business.Core.Scrapers;

namespace Sherlock.Business.Core.Base;
public class Comparator
{
    public SearchResult Compare(List<SearchResult> preResults)
    {
        // lógica de comparação dos resultados
        // pode ser baseada em critérios como preço, avaliação, relevância, etc.
        // retornar o resultado mais relevante
        // por enquanto, apenas preco mais baixo

        if (preResults == null || preResults.Count == 0)
            return new SearchResult();

        preResults = preResults.Where(r => r.Book != null).ToList();
        return preResults.OrderBy(r => r.Book.Price).First();
    }
}
