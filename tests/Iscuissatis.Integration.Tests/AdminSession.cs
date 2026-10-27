using System.Net;
using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

/// <summary>
/// Cria um administrador descartável com 2FA já ativo e faz o login completo
/// (senha + TOTP). Necessário porque o painel agora exige a segunda etapa.
/// </summary>
public static class AdminSession
{
    public const string Password = "SenhaDoPainel123";

    public static async Task<HttpClient> SignInAsync(IntegrationTestFixture fixture)
    {
        var email = $"admin.{Guid.NewGuid():N}@teste.local";
        var key = await CreateAdminWithTwoFactorAsync(fixture, email);

        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var login = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/entrar", "/entrar",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = Password,
                ["RememberMe"] = "false",
            });

        login.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(login).ShouldBe("/entrar/dois-fatores");

        var secondFactor = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/entrar/dois-fatores", "/entrar/dois-fatores",
            new Dictionary<string, string> { ["Code"] = Totp.Compute(key) });

        secondFactor.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        return client;
    }

    private static async Task<string> CreateAdminWithTwoFactorAsync(IntegrationTestFixture fixture, string email)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Admin de Teste",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        (await users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        (await users.AddToRoleAsync(user, Roles.Admin)).Succeeded.ShouldBeTrue();
        (await users.ResetAuthenticatorKeyAsync(user)).Succeeded.ShouldBeTrue();

        var key = await users.GetAuthenticatorKeyAsync(user);
        key.ShouldNotBeNullOrWhiteSpace();

        (await users.SetTwoFactorEnabledAsync(user, true)).Succeeded.ShouldBeTrue();

        return key!;
    }
}
