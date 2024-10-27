using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.Base;
// essa classe deve:
// - organizar a chamada da consulta (seja ela unica ou conjugada)
// - verificar se os resultados ja existem em banco 
// - calcular custo da transacao
// - atualizar registros no banco
public class CoreExecutor
{
    public async Task<SearchResult> ExecuteTransaction(Requestor requestor)
    {
        // todo 
        var consulta = new CedetSingleSearch();

        var result = await consulta.ExecuteMainLoop((CedetSingleSearchParams)requestor.SearchParameters);
        
        return result;
    }
}
