namespace Sherlock.Domain.Entities;

/// <summary>
/// Esta classe representa uma consulta realizada para obter informações sobre preços de livros.
/// </summary>
public class Query
{
    public int Id { get; set; }
    public int ProviderTypeId { get; set; }
    public int ResultTypeId { get; set; }
    public int TokenId { get; set; }
    public int BookId { get; set; }
    public DateTime StartDateTime { get; set; } 
    public DateTime EndDateTime { get; set; }
    public string InputParameters { get; set; }
    public string Result { get; set; }
}
