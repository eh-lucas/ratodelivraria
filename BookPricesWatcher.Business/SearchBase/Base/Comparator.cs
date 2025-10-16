using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sherlock.Business.SearchBase.Runners.Cedet;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.Base;
public class Comparator
{
    public SearchResult Compare(List<CedetSingleSearchResult> preResults)
    {
        // lógica de comparação dos resultados
        // pode ser baseada em critérios como preço, avaliação, relevância, etc.
        // retornar o resultado mais relevante
        // por enquanto, apenas preco mais baixo
        if (preResults == null || preResults.Count == 0)
            return new CedetSingleSearchResult();

        preResults = preResults.Where(r => r.Book != null).ToList();
        return preResults.OrderBy(r => r.Book.Price).First();
    }
}
