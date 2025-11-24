namespace Sherlock.Domain.Entities;
public class ResultType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsBillable { get; set; }

    public static ResultType Success => new()
    {
        Name = "Success",
        Description = "Busca realizada com sucesso",
        IsSuccess = true,
        IsBillable = true
    };

    public static ResultType PartialSuccess => new()
    {
        Name = "PartialSuccess",
        Description = "Busca parcialmente realizada - alguns providers falharam",
        IsSuccess = true,
        IsBillable = true
    };

    public static ResultType NoResults => new()
    {
        Name = "NoResults",
        Description = "Nenhum resultado encontrado",
        IsSuccess = false,
        IsBillable = false
    };

    public static ResultType AllFailed => new()
    {
        Name = "AllFailed",
        Description = "Todos os providers falharam",
        IsSuccess = false,
        IsBillable = false
    };
}
