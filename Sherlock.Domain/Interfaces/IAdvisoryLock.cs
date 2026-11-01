namespace Sherlock.Domain.Interfaces;

/// <summary>
/// Trava nomeada no banco, para que duas instâncias da API não façam o mesmo
/// trabalho de fundo ao mesmo tempo.
///
/// É interface porque o teste não precisa de Postgres para exercitar o worker, e
/// porque a varredura dobrada que ela evita acontece do lado das livrarias — o
/// custo de errar aqui não é nosso.
/// </summary>
public interface IAdvisoryLock
{
    /// <summary>
    /// Tenta travar sem esperar. Devolve null se outra instância já tem a trava;
    /// descartar o retorno solta.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(long key, CancellationToken cancellationToken = default);
}
