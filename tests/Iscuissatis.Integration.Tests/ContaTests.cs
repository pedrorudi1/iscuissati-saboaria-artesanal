using System.Net;
using Iscuissatis.Application.Identity;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class ContaTests(IntegrationTestFixture fixture)
{
    private const string Password = "SenhaDoCliente123";

    [Fact]
    public async Task Registering_creates_a_customer_and_merges_the_guest_cart()
    {
        var (client, variantId) = await GuestWithItemAsync();
        var email = $"cliente.{Guid.NewGuid():N}@teste.local";

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/criar-conta", "/criar-conta",
            new Dictionary<string, string>
            {
                ["FullName"] = "Cliente Novo",
                ["Email"] = email,
                ["Password"] = Password,
                ["ConfirmPassword"] = Password,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        AuthHelper.PathOf(response).ShouldBe("/minha-conta");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await users.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        (await users.IsInRoleAsync(user, Roles.Cliente)).ShouldBeTrue();

        var customerId = Guid.Parse(user.Id);
        var cart = await db.Carts.Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.CustomerId == customerId);

        cart.ShouldNotBeNull();
        cart!.Items.Single().ProductVariantId.ShouldBe(variantId);
    }

    [Fact]
    public async Task Customer_cannot_open_an_order_from_someone_else()
    {
        var ownerEmail = $"dono.{Guid.NewGuid():N}@teste.local";
        var otherEmail = $"outro.{Guid.NewGuid():N}@teste.local";

        var ownerId = await CreateCustomerAsync(ownerEmail);
        await CreateCustomerAsync(otherEmail);

        var orderNumber = await SeedOrderAsync(ownerId);

        var client = await AuthHelper.SignInAsync(fixture.Factory, otherEmail, Password);
        var response = await client.GetAsync($"/minha-conta/pedidos/{orderNumber}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customer_sees_only_their_own_orders()
    {
        var ownerEmail = $"lista.{Guid.NewGuid():N}@teste.local";
        var ownerId = await CreateCustomerAsync(ownerEmail);
        var orderNumber = await SeedOrderAsync(ownerId);

        var client = await AuthHelper.SignInAsync(fixture.Factory, ownerEmail, Password);
        var html = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/minha-conta")).Content.ReadAsStringAsync());

        html.ShouldContain(orderNumber);
    }

    private async Task<Guid> CreateCustomerAsync(string email)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Cliente de Teste",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        (await users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        (await users.AddToRoleAsync(user, Roles.Cliente)).Succeeded.ShouldBeTrue();

        return Guid.Parse(user.Id);
    }

    private async Task<string> SeedOrderAsync(Guid customerId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var address = ShippingAddress.Create("Cliente", "Rua A", "1", null, "Centro", "Tatuí", "SP", "18300000");

        var order = Order.Create(
            $"T{DateTime.UtcNow:yyyyMMdd}{suffix}".ToUpperInvariant(),
            customerId,
            null,
            address,
            [(Guid.NewGuid(), $"Sabonete {suffix}", "100 g", 24.90m, 1)],
            DateTimeOffset.UtcNow);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order.Number;
    }

    private async Task<(HttpClient Client, Guid VariantId)> GuestWithItemAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        Guid variantId;

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = Category.Create($"Conta {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Product.Create($"Sabonete Conta {suffix}", category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"CON-{suffix}", 24.90m, 5, 100, DateTimeOffset.UtcNow);
            product.AddImage($"/media/teste/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);
            product.Publish(DateTimeOffset.UtcNow);

            db.Products.Add(product);
            await db.SaveChangesAsync();
            variantId = variant.Id;
        }

        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/sacola", "/sacola/adicionar",
            new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "1" });

        return (client, variantId);
    }
}
