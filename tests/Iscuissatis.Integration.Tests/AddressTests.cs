using System.Net;
using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class AddressTests(IntegrationTestFixture fixture)
{
    private const string Password = "SenhaDoCliente123";

    [Fact]
    public async Task First_address_becomes_default_and_prefills_the_checkout()
    {
        var email = await CreateCustomerAsync();
        var client = await AuthHelper.SignInAsync(fixture.Factory, email, Password);

        var response = await AuthHelper.PostWithAntiforgeryAsync(
            client, "/minha-conta/enderecos", "/minha-conta/enderecos",
            new Dictionary<string, string>
            {
                ["Label"] = "Casa",
                ["Recipient"] = "Maria Silva",
                ["ZipCode"] = "18300-000",
                ["Street"] = "Rua das Flores",
                ["Number"] = "123",
                ["District"] = "Centro",
                ["City"] = "Tatuí",
                ["State"] = "SP",
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        var customerId = Guid.Parse(user!.Id);

        var address = await db.Addresses.SingleAsync(item => item.CustomerId == customerId);
        savedAddressIsDefault(address.IsDefault);
        address.Label.ShouldBe("Casa");

        // O checkout usa o endereço padrão do cliente
        await AddItemToCartAsync(client);
        var checkout = System.Net.WebUtility.HtmlDecode(
            await (await client.GetAsync("/checkout")).Content.ReadAsStringAsync());

        checkout.ShouldContain("Rua das Flores");
        checkout.ShouldContain("Tatuí");
    }

    [Fact]
    public async Task Setting_another_address_as_default_moves_the_flag()
    {
        var email = await CreateCustomerAsync();
        var client = await AuthHelper.SignInAsync(fixture.Factory, email, Password);

        foreach (var (label, street) in new[] { ("Casa", "Rua A"), ("Trabalho", "Rua B") })
        {
            await AuthHelper.PostWithAntiforgeryAsync(
                client, "/minha-conta/enderecos", "/minha-conta/enderecos",
                new Dictionary<string, string>
                {
                    ["Label"] = label,
                    ["Recipient"] = "Maria Silva",
                    ["ZipCode"] = "18300000",
                    ["Street"] = street,
                    ["Number"] = "10",
                    ["District"] = "Centro",
                    ["City"] = "Tatuí",
                    ["State"] = "SP",
                });
        }

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customerId = Guid.Parse((await users.FindByEmailAsync(email))!.Id);

        var work = await db.Addresses.SingleAsync(item => item.CustomerId == customerId && item.Label == "Trabalho");

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/minha-conta/enderecos", $"/minha-conta/enderecos/{work.Id}/padrao",
            new Dictionary<string, string>());

        using var verifyScope = fixture.Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var addresses = await verifyDb.Addresses.Where(item => item.CustomerId == customerId).ToListAsync();

        addresses.Count(item => item.IsDefault).ShouldBe(1);
        addresses.Single(item => item.IsDefault).Label.ShouldBe("Trabalho");
    }

    private static void savedAddressIsDefault(bool isDefault) => isDefault.ShouldBeTrue();

    private async Task<string> CreateCustomerAsync()
    {
        var email = $"endereco.{Guid.NewGuid():N}@teste.local";

        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Cliente Endereço",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        (await users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        (await users.AddToRoleAsync(user, Roles.Cliente)).Succeeded.ShouldBeTrue();

        return email;
    }

    private async Task AddItemToCartAsync(HttpClient client)
    {
        Guid variantId;

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var suffix = Guid.NewGuid().ToString("N")[..6];

            var category = Iscuissatis.Domain.Catalog.Category.Create($"End {suffix}", DateTimeOffset.UtcNow);
            db.Categories.Add(category);

            var product = Iscuissatis.Domain.Catalog.Product.Create($"Sabonete End {suffix}", category.Id, DateTimeOffset.UtcNow);
            var variant = product.AddVariant("100 g", $"END-{suffix}", 10m, 5, 100, DateTimeOffset.UtcNow);
            product.AddImage($"/media/{suffix}.webp", "Foto", DateTimeOffset.UtcNow);
            product.Publish(DateTimeOffset.UtcNow);

            db.Products.Add(product);
            await db.SaveChangesAsync();
            variantId = variant.Id;
        }

        await AuthHelper.PostWithAntiforgeryAsync(
            client, "/sacola", "/sacola/adicionar",
            new Dictionary<string, string> { ["variantId"] = variantId.ToString(), ["quantity"] = "1" });
    }
}
