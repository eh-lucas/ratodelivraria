namespace Sherlock.Api.Configurations;

/// <summary>
/// Opções do "modo demo" (branch de apresentação): quando habilitado, a API
/// autentica toda requisição como um usuário master, dispensando login.
/// Default desabilitado — em produção (main) o fluxo normal de JWT continua valendo.
/// </summary>
public class DemoModeOptions
{
    public const string SectionName = "DemoMode";

    /// <summary>Liga/desliga o modo demo. Default: false.</summary>
    public bool Enabled { get; set; }

    /// <summary>Email do usuário master criado/garantido no startup.</summary>
    public string MasterUserEmail { get; set; } = "demo@sherlock.local";

    /// <summary>Nome exibido do usuário master.</summary>
    public string MasterUsername { get; set; } = "Convidado";

    /// <summary>Créditos atribuídos ao master no startup (efetivamente ilimitado para a demo).</summary>
    public int MasterCredits { get; set; } = 1_000_000_000;
}
