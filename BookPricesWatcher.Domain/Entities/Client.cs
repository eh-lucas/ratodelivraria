namespace Sherlock.Domain.Entities;
public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime DeletionDate { get; set; }
    public long CpfCnpj { get; set; }
}
