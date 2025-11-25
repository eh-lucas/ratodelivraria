namespace Sherlock.Domain.Entities;

/// <summary>
/// Representa uma transação de busca de preços.
/// Agrega múltiplas queries individuais feitas a diferentes providers.
/// </summary>
public class Transaction
{
    public int Id { get; set; }

    /// <summary>
    /// Usuário que realizou a transação (opcional para buscas anônimas)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Livro buscado (opcional, pode ser preenchido após identificação)
    /// </summary>
    public int? BookId { get; set; }

    /// <summary>
    /// Tipo de resultado da transação (Success, PartialSuccess, NoResults, AllFailed)
    /// </summary>
    public int ResultTypeId { get; set; }

    /// <summary>
    /// Início da transação
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fim da transação
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Tempo total de execução em milissegundos
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Total de providers consultados
    /// </summary>
    public int TotalProvidersQueried { get; set; }

    /// <summary>
    /// Consultas com resultado válido
    /// </summary>
    public int SuccessfulQueries { get; set; }

    /// <summary>
    /// Consultas que falharam
    /// </summary>
    public int FailedQueries { get; set; }

    /// <summary>
    /// Custo em créditos da transação
    /// </summary>
    public int CostCredits { get; set; }

    /// <summary>
    /// Parâmetros de entrada (título, ISBN, autor buscados) em JSON
    /// </summary>
    public string InputParameters { get; set; } = string.Empty;

    /// <summary>
    /// Se o resultado veio do cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// Erros gerais da transação em JSON
    /// </summary>
    public string? Errors { get; set; }

    /// <summary>
    /// ID da query que retornou o melhor resultado
    /// </summary>
    public int? BestQueryId { get; set; }

    // Navegação
    public User? User { get; set; }
    public Book? Book { get; set; }
    public ResultType? ResultType { get; set; }
    public Query? BestQuery { get; set; }
    public ICollection<Query> Queries { get; set; } = new List<Query>();
}
