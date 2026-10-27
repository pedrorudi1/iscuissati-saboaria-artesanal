namespace Iscuissatis.Application.Identity;

/// <summary>
/// Papéis do sistema. Cliente compra; Operador e Admin operam o painel.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Operador = "Operador";
    public const string Cliente = "Cliente";

    public static readonly string[] All = [Admin, Operador, Cliente];

    /// <summary>Papéis que podem entrar na área administrativa.</summary>
    public const string AdminArea = Admin + "," + Operador;

    public static bool IsAdminArea(string? role) => role is Admin or Operador;
}
