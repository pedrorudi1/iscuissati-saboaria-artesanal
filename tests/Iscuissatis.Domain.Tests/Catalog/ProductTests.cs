using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Shouldly;

namespace Iscuissatis.Domain.Tests.Catalog;

public class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Product NewProduct(string name = "Sabonete de Lavanda")
        => Product.Create(name, CategoryId, Now, description: "Barra de 100 g");

    [Fact]
    public void Create_generates_slug_from_name()
    {
        var product = NewProduct();

        product.Slug.ShouldBe("sabonete-de-lavanda");
        product.Status.ShouldBe(ProductStatus.Draft);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_name_without_letters(string name)
    {
        Should.Throw<DomainException>(() => NewProduct(name));
    }

    [Fact]
    public void Create_rejects_empty_category()
    {
        Should.Throw<DomainException>(() => Product.Create("Sabonete", Guid.Empty, Now));
    }

    [Fact]
    public void Create_rejects_negative_net_weight()
    {
        Should.Throw<DomainException>(() =>
            Product.Create("Sabonete", CategoryId, Now, netWeightGrams: -1));
    }

    [Fact]
    public void Update_details_keeps_the_slug_stable()
    {
        var product = NewProduct();

        product.UpdateDetails("Sabonete de Lavanda Premium", CategoryId, Now.AddDays(1), "Nova descrição");

        product.Name.ShouldBe("Sabonete de Lavanda Premium");
        product.Slug.ShouldBe("sabonete-de-lavanda"); // URLs antigas continuam válidas
    }

    [Fact]
    public void Publish_requires_an_active_variant()
    {
        var product = NewProduct();
        product.AddImage("/img/lavanda.webp", "Sabonete de lavanda", Now);

        Should.Throw<DomainException>(() => product.Publish(Now))
            .Message.ShouldContain("variação");
    }

    [Fact]
    public void Publish_requires_an_image()
    {
        var product = NewProduct();
        product.AddVariant("Lavanda 100g", "LAV-100", 24.90m, 10, 100, Now);

        Should.Throw<DomainException>(() => product.Publish(Now))
            .Message.ShouldContain("imagem");
    }

    [Fact]
    public void Publish_requires_variant_that_is_active()
    {
        var product = NewProduct();
        var variant = product.AddVariant("Lavanda 100g", "LAV-100", 24.90m, 10, 100, Now);
        variant.Deactivate();
        product.AddImage("/img/lavanda.webp", "Sabonete de lavanda", Now);

        Should.Throw<DomainException>(() => product.Publish(Now));
    }

    [Fact]
    public void Publish_sets_status_when_requirements_are_met()
    {
        var product = NewProduct();
        product.AddVariant("Lavanda 100g", "LAV-100", 24.90m, 10, 100, Now);
        product.AddImage("/img/lavanda.webp", "Sabonete de lavanda", Now);

        product.Publish(Now);

        product.Status.ShouldBe(ProductStatus.Published);
        product.PublishedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Unpublish_returns_to_draft()
    {
        var product = NewProduct();
        product.AddVariant("Lavanda 100g", "LAV-100", 24.90m, 10, 100, Now);
        product.AddImage("/img/lavanda.webp", "Sabonete de lavanda", Now);
        product.Publish(Now);

        product.Unpublish(Now);

        product.Status.ShouldBe(ProductStatus.Draft);
    }

    [Fact]
    public void AddVariant_rejects_duplicated_sku_inside_the_same_product()
    {
        var product = NewProduct();
        product.AddVariant("Lavanda 100g", "LAV-100", 24.90m, 10, 100, Now);

        Should.Throw<DomainException>(() => product.AddVariant("Lavanda 200g", "lav-100", 39.90m, 5, 200, Now));
    }

    [Fact]
    public void First_image_becomes_the_primary_one()
    {
        var product = NewProduct();

        var first = product.AddImage("/img/1.webp", "Frente", Now);
        var second = product.AddImage("/img/2.webp", "Verso", Now);

        first.IsPrimary.ShouldBeTrue();
        second.IsPrimary.ShouldBeFalse();
    }
}
