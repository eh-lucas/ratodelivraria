using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sherlock.Business.SearchBase.SearchTypes;
using Sherlock.Business.SearchBase.SearchTypes.Cedet;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.SearchBase.Base;
public static class TransactionExecutor
{
    public static async Task<T> ExecuteTransaction(Requestor requestor)
    {
        var consulta = new CedetSingleSearch();

        var result = await consulta.Execute(requestor, consulta);
        return result[0];
    }
}
