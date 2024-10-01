namespace API.Domain;
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime DeletionDate { get; set; }
    public long Cpf { get; set; }
    public string Password { get; set; }
}
