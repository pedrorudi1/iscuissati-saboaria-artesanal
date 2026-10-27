namespace Iscuissatis.Infrastructure.Identity;

/// <summary>
/// Credenciais do primeiro administrador, vindas de variáveis de ambiente
/// (Seed__AdminEmail, Seed__AdminPassword, Seed__AdminFullName). Nunca do código.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }

    public string? AdminFullName { get; set; }
}
