using Iscuissatis.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Iscuissatis.Infrastructure.Identity;

/// <summary>
/// Garante os papéis do sistema e o usuário administrador inicial.
/// É idempotente: pode rodar em todo start sem duplicar dados.
/// </summary>
public sealed class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<SeedOptions> seedOptions,
    TimeProvider timeProvider,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            EnsureSucceeded(result, $"criar o papel {role}");
        }
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var options = seedOptions.Value;

        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            logger.LogWarning(
                "Seed do admin ignorado: defina Seed__AdminEmail e Seed__AdminPassword para criar o primeiro administrador.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(options.AdminEmail);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, Roles.Admin))
            {
                await userManager.AddToRoleAsync(existing, Roles.Admin);
            }

            return;
        }

        var admin = new ApplicationUser
        {
            UserName = options.AdminEmail,
            Email = options.AdminEmail,
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(options.AdminFullName) ? "Administrador" : options.AdminFullName,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        var created = await userManager.CreateAsync(admin, options.AdminPassword);
        EnsureSucceeded(created, "criar o usuário administrador");

        var added = await userManager.AddToRoleAsync(admin, Roles.Admin);
        EnsureSucceeded(added, "adicionar o administrador ao papel Admin");

        logger.LogInformation("Administrador inicial criado para {Email}.", admin.Email);
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Falha ao {action}. {errors}");
    }
}
