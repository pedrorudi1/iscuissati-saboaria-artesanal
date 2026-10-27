using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace Iscuissatis.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.AdminArea)]
public class SegurancaController(UserManager<ApplicationUser> userManager) : Controller
{
    private const string Issuer = "Iscuissati's Saboaria";
    private const string RecoveryCodesKey = "RecoveryCodes";

    [HttpGet("/admin/seguranca/2fa")]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        if (TempData["TwoFactorError"] is string error)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var model = new AuthenticatorSetupViewModel
        {
            SharedKey = FormatKey(key!),
            AuthenticatorUri = BuildAuthenticatorUri(user.Email ?? user.UserName ?? "admin", key!),
            RecoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user),
            IsEnabled = user.TwoFactorEnabled,
            ReturnUrl = returnUrl,
        };

        model.QrCodeDataUri = BuildQrCodeDataUri(model.AuthenticatorUri);
        return View(model);
    }

    [HttpPost("/admin/seguranca/2fa/ativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(string code, string? returnUrl = null)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var normalized = (code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider, normalized);

        if (!isValid)
        {
            TempData["TwoFactorError"] = "Código inválido. Confira o horário do celular e tente o código atual.";
            return RedirectToAction(nameof(Index), new { returnUrl });
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData[RecoveryCodesKey] = string.Join('\n', recoveryCodes ?? []);

        return RedirectToAction(nameof(RecoveryCodes));
    }

    [HttpGet("/admin/seguranca/codigos-recuperacao")]
    public IActionResult RecoveryCodes()
    {
        if (TempData[RecoveryCodesKey] is not string codes || string.IsNullOrWhiteSpace(codes))
        {
            return RedirectToAction(nameof(Index));
        }

        // Mantém os códigos disponíveis se a página for recarregada.
        TempData.Keep(RecoveryCodesKey);

        return View(codes.Split('\n'));
    }

    [HttpPost("/admin/seguranca/codigos-recuperacao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateRecoveryCodes()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData[RecoveryCodesKey] = string.Join('\n', recoveryCodes ?? []);

        return RedirectToAction(nameof(RecoveryCodes));
    }

    private static string BuildAuthenticatorUri(string email, string key)
        => $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(email)}"
           + $"?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6&period=30";

    private static string BuildQrCodeDataUri(string authenticatorUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(10);

        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }

    private static string FormatKey(string key)
        => string.Join(' ', Enumerable.Range(0, (key.Length + 3) / 4)
            .Select(index => key.Substring(index * 4, Math.Min(4, key.Length - index * 4))));
}

public class AuthenticatorSetupViewModel
{
    public string SharedKey { get; set; } = string.Empty;

    public string AuthenticatorUri { get; set; } = string.Empty;

    public string QrCodeDataUri { get; set; } = string.Empty;

    public int RecoveryCodesLeft { get; set; }

    public bool IsEnabled { get; set; }

    public string? ReturnUrl { get; set; }
}
