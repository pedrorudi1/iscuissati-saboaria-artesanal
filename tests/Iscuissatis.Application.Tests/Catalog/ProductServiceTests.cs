using Iscuissatis.Application.Catalog;
using Iscuissatis.Application.Storage;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Iscuissatis.Application.Tests.Catalog;

public class ProductServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly IImageProcessor _images = Substitute.For<IImageProcessor>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _products.SlugExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _products.SkuExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _categories.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Category.Create("Sabonetes", Now));

        _service = new ProductService(_products, _categories, _clock, _storage, _images);
    }

    [Fact]
    public async Task Create_generates_slug_and_keeps_the_product_as_draft()
    {
        var product = await _service.CreateAsync(CategoryId, "Sabonete de Lavanda");

        product.Slug.ShouldBe("sabonete-de-lavanda");
        product.Status.ShouldBe(ProductStatus.Draft);
        _products.Received(1).Add(product);
        await _products.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_suffixes_the_slug_when_it_already_exists()
    {
        _products.SlugExistsAsync("sabonete-de-lavanda", null, Arg.Any<CancellationToken>()).Returns(true);

        var product = await _service.CreateAsync(CategoryId, "Sabonete de Lavanda");

        product.Slug.ShouldBe("sabonete-de-lavanda-2");
    }

    [Fact]
    public async Task Create_rejects_a_category_that_does_not_exist()
    {
        _categories.FindAsync(CategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        await Should.ThrowAsync<DomainException>(() => _service.CreateAsync(CategoryId, "Sabonete"));

        _products.DidNotReceive().Add(Arg.Any<Product>());
    }

    [Fact]
    public async Task AddVariant_saves_and_normalizes_the_sku()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var variant = await _service.AddVariantAsync(
            product.Id, "Lavanda 100g", "lav-100", 24.90m, 10, 100);

        variant.Sku.ShouldBe("LAV-100");
        product.Variants.Count.ShouldBe(1);
        await _products.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddVariant_rejects_sku_used_by_another_product()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _products.SkuExistsAsync("LAV-100", null, Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<DomainException>(() => _service.AddVariantAsync(
            product.Id, "Lavanda 100g", "lav-100", 24.90m, 10, 100));
    }

    [Fact]
    public async Task Publish_reports_the_missing_image_when_variant_exists()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        product.AddVariant("Lavanda", "LAV-1", 10m, 5, 100, Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var error = await Should.ThrowAsync<DomainException>(() => _service.PublishAsync(product.Id));

        error.Message.ShouldContain("imagem");
    }

    [Fact]
    public async Task Publish_sets_status_when_the_product_is_complete()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        product.AddVariant("Lavanda", "LAV-2", 10m, 5, 100, Now);
        product.AddImage("/img/a.webp", "Sabonete de lavanda", Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _service.PublishAsync(product.Id);

        product.Status.ShouldBe(ProductStatus.Published);
    }

    [Fact]
    public async Task AdjustStock_refuses_to_leave_the_stock_negative()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        var variant = product.AddVariant("Lavanda", "LAV-3", 10m, 3, 100, Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await Should.ThrowAsync<DomainException>(() =>
            _service.AdjustStockAsync(product.Id, variant.Id, -5));

        variant.StockQuantity.ShouldBe(3);
    }

    [Fact]
    public async Task Update_details_keeps_the_slug_stable()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _categories.FindAsync(CategoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create("Sabonetes", Now));

        await _service.UpdateAsync(product.Id, CategoryId, "Sabonete de Lavanda Premium");

        product.Name.ShouldBe("Sabonete de Lavanda Premium");
        product.Slug.ShouldBe("sabonete");
    }

    [Fact]
    public async Task AddImage_stores_the_processed_file_and_registers_it()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _images.ProcessAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessedImage([1, 2, 3], "image/webp", 800, 600));
        _storage.SaveAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string>(), "image/webp", Arg.Any<CancellationToken>())
            .Returns("/media/produtos/2026/10/abc.webp");

        using var stream = new MemoryStream([1, 2, 3]);
        var image = await _service.AddImageAsync(product.Id, stream, "foto.jpeg", "Sabonete de lavanda");

        image.Path.ShouldBe("/media/produtos/2026/10/abc.webp");
        image.Width.ShouldBe(800);
        image.Height.ShouldBe(600);
        image.IsPrimary.ShouldBeTrue();
        await _storage.Received(1).SaveAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Is<string>(name => name.EndsWith(".webp")),
            "image/webp", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddImage_rejects_content_that_is_not_an_image()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _images.ProcessAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns((ProcessedImage?)null);

        using var stream = new MemoryStream([1, 2, 3]);

        await Should.ThrowAsync<DomainException>(() =>
            _service.AddImageAsync(product.Id, stream, "nota.jpg", "Arquivo falso"));

        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveImage_deletes_the_file_after_saving()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        var image = product.AddImage("/media/a.webp", "Frente", Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _service.RemoveImageAsync(product.Id, image.Id);

        product.Images.ShouldBeEmpty();
        await _storage.Received(1).DeleteAsync("/media/a.webp", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPrimaryImage_moves_the_flag()
    {
        var product = Product.Create("Sabonete", CategoryId, Now);
        var first = product.AddImage("/media/a.webp", "Frente", Now);
        var second = product.AddImage("/media/b.webp", "Verso", Now);
        _products.FindAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _service.SetPrimaryImageAsync(product.Id, second.Id);

        first.IsPrimary.ShouldBeFalse();
        second.IsPrimary.ShouldBeTrue();
    }

    [Fact]
    public async Task List_passes_the_filters_to_the_repository()
    {
        var expected = new List<Product> { Product.Create("Sabonete", CategoryId, Now) };
        _products.ListAsync("lavanda", ProductStatus.Published, CategoryId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _service.ListAsync("lavanda", ProductStatus.Published, CategoryId);

        result.ShouldBe(expected);
    }
}
