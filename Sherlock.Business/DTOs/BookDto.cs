namespace Sherlock.Business.DTOs;

public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Editor { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string Language { get; set; } = "pt-BR";
}
