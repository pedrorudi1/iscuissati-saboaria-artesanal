using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Iscuissatis.Web.Infrastructure;

/// <summary>
/// Enquanto o administrador não configurar o 2FA, ele não usa o painel:
/// qualquer rota /admin o leva para a tela de configuração do autenticador.
/// </summary>
public sealed class RequireAdminTwoFactorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (ShouldEnforce(context))
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is not null && !user.TwoFactorEnabled && await IsAdminAreaAsync(userManager, user))
            {
                context.Response.Redirect("/admin/seguranca/2fa");
                return;
            }
        }

        await next(context);
    }

    private static bool ShouldEnforce(HttpContext context)
        => context.User.Identity?.IsAuthenticated == true
           && context.Request.Path.StartsWithSegments("/admin")
           && !context.Request.Path.StartsWithSegments("/admin/seguranca");

    private static async Task<bool> IsAdminAreaAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
        => await userManager.IsInRoleAsync(user, Roles.Admin)
           || await userManager.IsInRoleAsync(user, Roles.Operador);
}
