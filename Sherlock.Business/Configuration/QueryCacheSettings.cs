namespace Sherlock.Business.Configuration;

/// <summary>
/// Configurações de cache para queries por ISBN
/// </summary>
public class QueryCacheSettings
{
    public const string SectionName = "QueryCache";

    /// <summary>
    /// Tempo padrão de cache em minutos (default: 30)
    /// </summary>
    public int DefaultCacheTimeMinutes { get; set; } = 30;
}
