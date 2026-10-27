using System.Net;
using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class TwoFactorTests(IntegrationTestFixture fixture)
{
    private const string Password = "SenhaDoPainel123";

    [Fact]
    public async Task Admin_without_two_factor_is_sent_to_setup_when_opening_the_panel()
    {
        var email = await CreateAdminAsync();
        var client = await AuthHelper.SignInAsync(fixture.Factory, email, Password);

        var response = await client.GetAsync("/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldBe("/admin/seguranca/2fa");
    }

    [Fact]
    public async Task Admin_enables_two_factor_and_then_signs_in_with_a_code()
    {
        var email = await CreateAdminAsync();
        var passwordClient = await AuthHelper.SignInAsync(fixture.Factory, email, Password);

        // 1. A tela de configuração mostra QR code e chave
        var setupPage = await passwordClient.GetAsync("/admin/seguranca/2fa");
        var setupHtml = await setupPage.Content.ReadAsStringAsync();
        setupPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        setupHtml.ShouldContain("Verificação em duas etapas");
        setupHtml.ShouldContain("data:image/png;base64,");

        // 2. O admin ativa com um código gerado da mesma chave que o app usaria
        var key = await GetAuthenticatorKeyAsync(email);
        var activation = await AuthHelper.PostWithAntiforgeryAsync(
            passwordClient, "/admin/seguranca/2fa", "/admin/seguranca/2fa/ativar",
            new Dictionary<string, string> { ["code"] = Totp.Compute(key!) });

        activation.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(activation).ShouldBe("/admin/seguranca/codigos-recuperacao");

        var user = await GetUserAsync(email);
        user.TwoFactorEnabled.ShouldBeTrue();
        (await CountRecoveryCodesAsync(user)).ShouldBeGreaterThan(0);

        // 3. Com 2FA ativo, o painel volta a abrir
        var dashboard = await passwordClient.GetAsync("/admin");
        dashboard.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 4. Um novo login exige a segunda etapa
        var loginClient = BrowserClient();
        var login = await AuthHelper.PostWithAntiforgeryAsync(
            loginClient, "/entrar", "/entrar",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = Password,
                ["RememberMe"] = "false",
            });

        login.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(login).ShouldBe("/entrar/dois-fatores");

        var challenge = await AuthHelper.PostWithAntiforgeryAsync(
            loginClient, "/entrar/dois-fatores", "/entrar/dois-fatores",
            new Dictionary<string, string> { ["Code"] = Totp.Compute(key!) });

        challenge.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var authenticatedDashboard = await loginClient.GetAsync("/admin");
        authenticatedDashboard.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await authenticatedDashboard.Content.ReadAsStringAsync()).ShouldContain("Área administrativa");
    }

    [Fact]
    public async Task Wrong_code_does_not_enable_two_factor()
    {
        var email = await CreateAdminAsync();
        var client = await AuthHelper.SignInAsync(fixture.Factory, email, Password);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/admin/seguranca/2fa", "/admin/seguranca/2fa/ativar",
            new Dictionary<string, string> { ["code"] = "000000" });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldBe("/admin/seguranca/2fa");

        var setupAfterError = await client.GetAsync("/admin/seguranca/2fa");
        (await setupAfterError.Content.ReadAsStringAsync()).ShouldContain("Código inválido");

        var user = await GetUserAsync(email);
        user.TwoFactorEnabled.ShouldBeFalse();
    }

    private HttpClient BrowserClient()
        => fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private async Task<string> CreateAdminAsync()
    {
        var email = $"admin.2fa.{Guid.NewGuid():N}@teste.local";

        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Admin 2FA",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        (await users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        (await users.AddToRoleAsync(user, Roles.Admin)).Succeeded.ShouldBeTrue();

        return email;
    }

    private async Task<string?> GetAuthenticatorKeyAsync(string email)
    {
        var user = await GetUserAsync(email);

        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.GetAuthenticatorKeyAsync(user);
    }

    private async Task<ApplicationUser> GetUserAsync(string email)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await users.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        return user;
    }

    private async Task<int> CountRecoveryCodesAsync(ApplicationUser user)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.CountRecoveryCodesAsync(user);
    }
}
