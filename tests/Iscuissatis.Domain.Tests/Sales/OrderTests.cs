using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;
using Shouldly;

namespace Iscuissatis.Domain.Tests.Sales;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid VariantA = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid VariantB = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static ShippingAddress Address()
        => ShippingAddress.Create("Maria Silva", "Rua das Flores", "123", null, "Centro", "Tatuí", "sp", "18.300-000");

    private static Order NewOrder(int quantityA = 2, int quantityB = 1)
        => Order.Create(
            "20261027-0001",
            null,
            "Cliente@Teste.com",
            Address(),
            [
                (VariantA, "Sabonete de Lavanda", "100 g", 24.90m, quantityA),
                (VariantB, "Sabonete de Calêndula", "100 g", 19.90m, quantityB),
            ],
            Now);

    [Fact]
    public void Address_normalizes_the_zip_code_and_state()
    {
        var address = Address();

        address.ZipCode.ShouldBe("18300000");
        address.State.ShouldBe("SP");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    public void Address_rejects_invalid_zip_code(string zipCode)
    {
        Should.Throw<DomainException>(() => ShippingAddress.Create(
            "Maria", "Rua A", "1", null, "Centro", "Tatuí", "SP", zipCode));
    }

    [Fact]
    public void Create_freezes_items_and_calculates_the_subtotal()
    {
        var order = NewOrder();

        order.Items.Count.ShouldBe(2);
        order.Subtotal.ShouldBe(69.70m);
        order.Total.ShouldBe(69.70m);
        order.Status.ShouldBe(OrderStatus.PendingPayment);
        order.GuestEmail.ShouldBe("cliente@teste.com");
    }

    [Fact]
    public void Create_requires_items()
    {
        Should.Throw<DomainException>(() => Order.Create(
            "20261027-0002", null, "cliente@teste.com", Address(), [], Now));
    }

    [Fact]
    public void Create_requires_customer_or_guest_email()
    {
        Should.Throw<DomainException>(() => Order.Create(
            "20261027-0003", null, null, Address(), [(VariantA, "Lavanda", "100 g", 10m, 1)], Now));
    }

    [Fact]
    public void Shipping_and_discount_change_the_total()
    {
        var order = NewOrder();

        order.SetShipping(15.90m, Now);
        order.ApplyDiscount(10m, Now);

        order.Total.ShouldBe(75.60m);
    }

    [Fact]
    public void Discount_cannot_exceed_the_total()
    {
        var order = NewOrder();

        Should.Throw<DomainException>(() => order.ApplyDiscount(1000m, Now));
    }

    [Fact]
    public void Happy_path_moves_through_every_status()
    {
        var order = NewOrder();

        order.MarkAsPaid(Now);
        order.StartPreparing(Now);
        order.Ship("BR123456789", Now);
        order.MarkAsDelivered(Now);

        order.Status.ShouldBe(OrderStatus.Delivered);
        order.PaidAtUtc.ShouldBe(Now);
        order.TrackingCode.ShouldBe("BR123456789");
    }

    [Fact]
    public void Shipping_before_preparing_is_refused()
    {
        var order = NewOrder();
        order.MarkAsPaid(Now);

        Should.Throw<DomainException>(() => order.Ship("BR1", Now));
    }

    [Fact]
    public void Paying_twice_is_refused()
    {
        var order = NewOrder();
        order.MarkAsPaid(Now);

        Should.Throw<DomainException>(() => order.MarkAsPaid(Now));
    }

    [Fact]
    public void Shipping_requires_a_tracking_code()
    {
        var order = NewOrder();
        order.MarkAsPaid(Now);
        order.StartPreparing(Now);

        Should.Throw<DomainException>(() => order.Ship("  ", Now));
    }

    [Fact]
    public void Cancel_is_refused_after_shipping()
    {
        var order = NewOrder();
        order.MarkAsPaid(Now);
        order.StartPreparing(Now);
        order.Ship("BR1", Now);

        Should.Throw<DomainException>(() => order.Cancel(Now));
    }

    [Fact]
    public void Cancel_works_while_waiting_payment()
    {
        var order = NewOrder();

        order.Cancel(Now);

        order.Status.ShouldBe(OrderStatus.Canceled);
        order.CanceledAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Refund_requires_a_paid_order()
    {
        var order = NewOrder();

        Should.Throw<DomainException>(() => order.Refund(Now));

        order.MarkAsPaid(Now);
        order.Refund(Now);
        order.Status.ShouldBe(OrderStatus.Refunded);
    }

    [Fact]
    public void Reservation_can_only_be_released_once()
    {
        var reservation = new StockReservation(Guid.NewGuid(), VariantA, 2, Now.AddMinutes(30));

        reservation.Release(Now);
        reservation.Release(Now.AddMinutes(5));

        reservation.IsReleased.ShouldBeTrue();
        reservation.ReleasedAtUtc.ShouldBe(Now);
    }
}
