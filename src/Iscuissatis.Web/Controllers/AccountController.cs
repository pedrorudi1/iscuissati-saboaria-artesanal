using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Iscuissatis.Web.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    Iscuissatis.Application.Sales.CartService cartService) : Controller
{
    [HttpGet("/entrar")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/entrar")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(TwoFactor), new { returnUrl = model.ReturnUrl, rememberMe = model.RememberMe });
        }

        if (result.Succeeded)
        {
            await MergeGuestCartAsync();

            if (await RequiresTwoFactorSetupAsync(model.Email))
            {
                return Redirect($"/admin/seguranca/2fa?returnUrl={Uri.EscapeDataString(model.ReturnUrl ?? "/admin")}");
            }

            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Conta temporariamente bloqueada por tentativas demais. Tente em 15 minutos.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
        return View(model);
    }

    [HttpGet("/entrar/dois-fatores")]
    [AllowAnonymous]
    public async Task<IActionResult> TwoFactor(string? returnUrl = null, bool rememberMe = false)
    {
        if (await signInManager.GetTwoFactorAuthenticationUserAsync() is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new TwoFactorViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe });
    }

    [HttpPost("/entrar/dois-fatores")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactor(TwoFactorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await signInManager.GetTwoFactorAuthenticationUserAsync() is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(code, model.RememberMe, rememberClient: false);

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Conta temporariamente bloqueada. Tente novamente em 15 minutos.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Código inválido ou expirado. Gere um novo código no aplicativo autenticador.");
        return View(model);
    }

    [HttpPost("/sair")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/criar-conta")]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterModel());

    [HttpPost("/criar-conta")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true, // a confirmação por e-mail entra na fase de e-mails
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, model.Password);

        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, Roles.Cliente);
        await signInManager.SignInAsync(user, isPersistent: false);
        await MergeGuestCartAsync();

        return RedirectToAction("Index", "Conta");
    }

    [HttpGet("/acesso-negado")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>
    /// Ao entrar na conta, a sacola do visitante passa para o cliente (spec 04, critério 5).
    /// </summary>
    private async Task MergeGuestCartAsync()
    {
        if (!Request.Cookies.TryGetValue(SacolaController.CookieName, out var anonymousId)
            || string.IsNullOrWhiteSpace(anonymousId))
        {
            return;
        }

        var user = await userManager.GetUserAsync(User);

        if (user is null || !Guid.TryParse(user.Id, out var customerId))
        {
            return;
        }

        await cartService.MergeAnonymousCartAsync(anonymousId, customerId);
        Response.Cookies.Delete(SacolaController.CookieName);
    }

    private async Task<bool> RequiresTwoFactorSetupAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.TwoFactorEnabled)
        {
            return false;
        }

        return await userManager.IsInRoleAsync(user, Roles.Admin)
            || await userManager.IsInRoleAsync(user, Roles.Operador);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }
}
