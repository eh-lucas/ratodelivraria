namespace API.Domain;
public class SearchType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> Sources { get; set; }
    public int Cost { get; set; }
}
