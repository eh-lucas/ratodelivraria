using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;

namespace Sherlock.Business.SearchBase.Base;
// essa classe deve:
// - organizar a chamada da consulta (seja ela unica ou conjugada)
// - verificar se os resultados ja existem em banco 
// - calcular custo da transacao
// - atualizar registros no banco
public class CoreExecutor
{
    public async Task<SearchResult> ExecuteTransaction<TConsulta, TParam, TResult>(Requestor requestor)
        where TConsulta : ConsultaBase<TParam, TResult>, new()
        where TParam : SearchParameters
        where TResult : SearchResult
    {
        var consulta = new TConsulta();
        
        var result = await consulta.ExecuteSearch((TParam)requestor.SearchParameters);

        return result;
    }
}
