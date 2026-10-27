using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;
using Shouldly;

namespace Iscuissatis.Domain.Tests.Sales;

public class CartTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid VariantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OtherVariantId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static Cart NewCart() => Cart.Create("anon-123", null, Now);

    private static void AddLavanda(Cart cart, int quantity, int stock = 10)
        => cart.AddItem(VariantId, "Sabonete de Lavanda", "100 g", 24.90m, quantity, stock, Now);

    [Fact]
    public void Create_requires_an_owner()
    {
        Should.Throw<DomainException>(() => Cart.Create(null, null, Now));
    }

    [Fact]
    public void AddItem_creates_a_line_with_frozen_name_and_price()
    {
        var cart = NewCart();

        AddLavanda(cart, 2);

        cart.Items.Count.ShouldBe(1);
        cart.Items.Single().Quantity.ShouldBe(2);
        cart.Items.Single().UnitPrice.ShouldBe(24.90m);
        cart.Items.Single().ProductName.ShouldBe("Sabonete de Lavanda");
    }

    [Fact]
    public void Adding_the_same_variant_again_sums_the_quantity_in_a_single_line()
    {
        var cart = NewCart();

        AddLavanda(cart, 1);
        AddLavanda(cart, 2);

        cart.Items.Count.ShouldBe(1);
        cart.Items.Single().Quantity.ShouldBe(3);
    }

    [Fact]
    public void Adding_above_stock_is_refused_and_keeps_the_previous_quantity()
    {
        var cart = NewCart();
        AddLavanda(cart, 2, stock: 3);

        Should.Throw<DomainException>(() => AddLavanda(cart, 2, stock: 3))
            .Message.ShouldContain("estoque");

        cart.Items.Single().Quantity.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Adding_non_positive_quantity_is_refused(int quantity)
    {
        var cart = NewCart();

        Should.Throw<DomainException>(() => AddLavanda(cart, quantity));
    }

    [Fact]
    public void Changing_quantity_to_zero_removes_the_line()
    {
        var cart = NewCart();
        AddLavanda(cart, 2);

        cart.ChangeQuantity(VariantId, 0, 10, Now);

        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_quantity_above_stock_is_refused()
    {
        var cart = NewCart();
        AddLavanda(cart, 1, stock: 3);

        Should.Throw<DomainException>(() => cart.ChangeQuantity(VariantId, 4, 3, Now));

        cart.Items.Single().Quantity.ShouldBe(1);
    }

    [Fact]
    public void Subtotal_multiplies_price_by_quantity_in_two_decimals()
    {
        var cart = NewCart();
        cart.AddItem(VariantId, "Lavanda", "100 g", 19.99m, 3, 10, Now);
        cart.AddItem(OtherVariantId, "Calêndula", "100 g", 5.50m, 2, 10, Now);

        cart.Subtotal.ShouldBe(70.97m);
        cart.ItemCount.ShouldBe(5);
    }

    [Fact]
    public void Empty_cart_has_zero_total()
    {
        NewCart().Subtotal.ShouldBe(0m);
    }

    [Fact]
    public void Merge_sums_equal_variants_and_keeps_distinct_ones()
    {
        var guest = NewCart();
        var customer = Cart.Create(null, Guid.NewGuid(), Now);

        guest.AddItem(VariantId, "Lavanda", "100 g", 24.90m, 2, 10, Now);
        customer.AddItem(VariantId, "Lavanda", "100 g", 24.90m, 1, 10, Now);
        customer.AddItem(OtherVariantId, "Calêndula", "100 g", 20m, 1, 10, Now);

        customer.MergeFrom(guest, _ => 10, Now);

        customer.Items.Count.ShouldBe(2);
        customer.Items.Single(item => item.ProductVariantId == VariantId).Quantity.ShouldBe(3);
    }

    [Fact]
    public void Merge_never_exceeds_the_available_stock()
    {
        var guest = NewCart();
        var customer = Cart.Create(null, Guid.NewGuid(), Now);

        guest.AddItem(VariantId, "Lavanda", "100 g", 24.90m, 3, 10, Now);
        customer.AddItem(VariantId, "Lavanda", "100 g", 24.90m, 2, 10, Now);

        customer.MergeFrom(guest, _ => 4, Now);

        customer.Items.Single().Quantity.ShouldBe(4);
    }
}
