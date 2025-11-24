namespace Sherlock.Domain.Entities;

/// <summary>
/// Representa uma consulta realizada para obter informações sobre preços de livros.
/// </summary>
public class Query
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int? BookId { get; set; }
    public int ResultTypeId { get; set; }
    public DateTime StartDateTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndDateTime { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int ProvidersQueried { get; set; }
    public int SuccessfulQueries { get; set; }
    public int FailedQueries { get; set; }
    public int CostCredits { get; set; }
    public string InputParameters { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}
