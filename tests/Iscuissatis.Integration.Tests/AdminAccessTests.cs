using System.Net;
using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class AdminAccessTests(IntegrationTestFixture fixture)
{
    private const string ClientEmail = "cliente@teste.local";
    private const string ClientPassword = "SenhaDoCliente123";

    [Fact]
    public async Task Seed_creates_admin_user_with_admin_role()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await users.FindByEmailAsync(IntegrationTestFixture.AdminEmail);

        admin.ShouldNotBeNull();
        admin.EmailConfirmed.ShouldBeTrue();
        (await users.IsInRoleAsync(admin, Roles.Admin)).ShouldBeTrue();
    }

    [Fact]
    public async Task Anonymous_visitor_is_redirected_to_login_when_opening_admin()
    {
        var client = fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.AbsolutePath.ShouldBe("/entrar");
        response.Headers.Location!.Query.ShouldContain("ReturnUrl");
    }

    [Fact]
    public async Task Customer_role_receives_403_when_opening_admin()
    {
        await EnsureCustomerAsync();
        var client = await AuthHelper.SignInAsync(fixture.Factory, ClientEmail, ClientPassword);

        var response = await client.GetAsync("/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_signs_in_and_reaches_the_admin_dashboard()
    {
        var client = await AuthHelper.SignInAsync(
            fixture.Factory, IntegrationTestFixture.AdminEmail, IntegrationTestFixture.AdminPassword);

        var response = await client.GetAsync("/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Área administrativa");
    }

    private async Task EnsureCustomerAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await users.FindByEmailAsync(ClientEmail) is not null)
        {
            return;
        }

        var customer = new ApplicationUser
        {
            UserName = ClientEmail,
            Email = ClientEmail,
            EmailConfirmed = true,
            FullName = "Cliente de Teste",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        (await users.CreateAsync(customer, ClientPassword)).Succeeded.ShouldBeTrue();
        (await users.AddToRoleAsync(customer, Roles.Cliente)).Succeeded.ShouldBeTrue();
    }
}
