using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Shouldly;

namespace Iscuissatis.Domain.Tests.Catalog;

public class ProductVariantTests
{
    private static ProductVariant NewVariant(
        decimal price = 24.90m,
        int stock = 10,
        decimal? promotionalPrice = null)
        => ProductVariant.Create(
            Guid.NewGuid(), "Lavanda 100g", "lav-100", price, stock, 100, 7.5m, 7.5m, 2.5m,
            promotionalPrice, DateTimeOffset.UtcNow);

    [Fact]
    public void Create_normalizes_sku_to_upper_case()
    {
        NewVariant().Sku.ShouldBe("LAV-100");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_rejects_empty_sku(string sku)
    {
        Should.Throw<DomainException>(() => ProductVariant.Create(
            Guid.NewGuid(), "Lavanda", sku, 10m, 1, 100, 1, 1, 1, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_rejects_negative_price()
    {
        Should.Throw<DomainException>(() => NewVariant(price: -0.01m));
    }

    [Fact]
    public void Create_accepts_zero_price_for_gifts()
    {
        NewVariant(price: 0m).Price.ShouldBe(0m);
    }

    [Fact]
    public void Create_rejects_negative_stock()
    {
        Should.Throw<DomainException>(() => NewVariant(stock: -1));
    }

    [Fact]
    public void Create_rejects_promotional_price_equal_or_greater_than_price()
    {
        Should.Throw<DomainException>(() => NewVariant(price: 24.90m, promotionalPrice: 24.90m));
        Should.Throw<DomainException>(() => NewVariant(price: 24.90m, promotionalPrice: 30m));
    }

    [Fact]
    public void Create_accepts_promotional_price_lower_than_price()
    {
        NewVariant(price: 24.90m, promotionalPrice: 19.90m).EffectivePrice.ShouldBe(19.90m);
    }

    [Fact]
    public void EffectivePrice_falls_back_to_price_without_promotion()
    {
        NewVariant(price: 24.90m).EffectivePrice.ShouldBe(24.90m);
    }

    [Fact]
    public void AdjustStock_decrements_when_there_is_enough_stock()
    {
        var variant = NewVariant(stock: 10);

        variant.AdjustStock(-4, DateTimeOffset.UtcNow);

        variant.StockQuantity.ShouldBe(6);
    }

    [Fact]
    public void AdjustStock_rejects_result_below_zero()
    {
        var variant = NewVariant(stock: 3);

        Should.Throw<DomainException>(() => variant.AdjustStock(-5, DateTimeOffset.UtcNow));
        variant.StockQuantity.ShouldBe(3);
    }

    [Fact]
    public void SetPromotionalPrice_rejects_value_above_price()
    {
        var variant = NewVariant(price: 24.90m);

        Should.Throw<DomainException>(() => variant.SetPromotionalPrice(25m, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Deactivate_hides_the_variant_from_the_storefront()
    {
        var variant = NewVariant();

        variant.Deactivate();

        variant.IsActive.ShouldBeFalse();
    }
}
