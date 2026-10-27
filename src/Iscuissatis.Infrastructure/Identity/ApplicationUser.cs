using Microsoft.AspNetCore.Identity;

namespace Iscuissatis.Infrastructure.Identity;

/// <summary>
/// Usuário autenticável do sistema. Clientes da loja e equipe do painel
/// compartilham a mesma tabela e se distinguem pelos papéis (Admin, Operador, Cliente).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
