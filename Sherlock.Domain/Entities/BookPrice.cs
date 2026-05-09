namespace Sherlock.Domain.Entities;

/// <summary>
/// Esta classe representa o preço de um livro obtido de um provedor específico em um determinado momento.
/// Ela será sobrescrita a cada nova consulta para manter um histórico atualizado dos preços.
/// </summary>
public class BookPrice
{
    public int Id { get; set; }
    public int LastQueryId { get; set; }
    public int BookId { get; set; }
    public int ProviderId { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public DateTime QueryDateTime { get; set; }
}
