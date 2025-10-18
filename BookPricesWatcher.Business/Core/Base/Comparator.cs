using Sherlock.Business.Core.Scrapers;

namespace Sherlock.Business.Core.Base;
public class Comparator
{
    public BookPriceResult Compare(List<BookPriceResult> preResults)
    {
        // lógica de comparação dos resultados
        // pode ser baseada em critérios como preço, avaliação, relevância, etc.
        // retornar o resultado mais relevante
        // por enquanto, apenas preco mais baixo

        if (preResults.Count == 0)
            return new BookPriceResult();

        preResults = preResults.Where(r => !string.IsNullOrEmpty(r.Name)).ToList();
        return preResults.OrderBy(r => r.Price).First();
    }
}
