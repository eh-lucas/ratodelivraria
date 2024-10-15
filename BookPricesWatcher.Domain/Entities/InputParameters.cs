namespace Sherlock.Domain.Entities;

/// <summary>
/// Dados de entrada inseridos pelo cliente para efetuar uma transação
/// </summary>
public class InputParameters
{
    public string BookTitle { get; set; }
    public string Token { get; set; }
    public InputParameters(string bookTitle, string token)
    {
        BookTitle = bookTitle;
        Token = token;
    }
    public InputParameters()
    {
    }
}
