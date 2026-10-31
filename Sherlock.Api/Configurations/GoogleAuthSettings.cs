namespace Sherlock.Api.Configurations;

/// <summary>
/// Entrada pelo Google. O <see cref="ClientId"/> é público — vai no HTML da página —,
/// e neste fluxo não existe client secret: o ID token é verificado por assinatura, não
/// por segredo compartilhado.
/// </summary>
public class GoogleAuthSettings
{
    public const string SectionName = "GoogleAuth";

    /// <summary>
    /// Vazio desliga o endpoint. É o padrão, para que o repo recém-clonado não ofereça
    /// um botão que não leva a lugar nenhum.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId);
}
