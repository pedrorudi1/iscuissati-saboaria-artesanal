using Iscuissatis.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Iscuissatis.Infrastructure.Persistence.Configurations;

public static class SalesConfigurations
{
    public static void ApplySales(this ModelBuilder builder)
    {
        builder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(cart => cart.Id);
            entity.Property(cart => cart.Id).ValueGeneratedNever();
            entity.Property(cart => cart.AnonymousId).HasMaxLength(64);
            entity.HasIndex(cart => cart.AnonymousId).IsUnique();
            entity.HasIndex(cart => cart.CustomerId).IsUnique();

            entity.HasMany(cart => cart.Items)
                .WithOne()
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata.FindNavigation(nameof(Cart.Items))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).ValueGeneratedNever();
            entity.Property(order => order.Number).HasMaxLength(32).IsRequired();
            entity.HasIndex(order => order.Number).IsUnique();
            entity.Property(order => order.GuestEmail).HasMaxLength(180);
            entity.Property(order => order.Subtotal).HasPrecision(18, 2);
            entity.Property(order => order.Shipping).HasPrecision(18, 2);
            entity.Property(order => order.Discount).HasPrecision(18, 2);
            entity.Ignore(order => order.Total);

            entity.OwnsOne(order => order.ShippingAddress, address =>
            {
                address.Property(item => item.Recipient).HasColumnName("ShipRecipient").HasMaxLength(160);
                address.Property(item => item.Street).HasColumnName("ShipStreet").HasMaxLength(200);
                address.Property(item => item.Number).HasColumnName("ShipNumber").HasMaxLength(20);
                address.Property(item => item.Complement).HasColumnName("ShipComplement").HasMaxLength(120);
                address.Property(item => item.District).HasColumnName("ShipDistrict").HasMaxLength(120);
                address.Property(item => item.City).HasColumnName("ShipCity").HasMaxLength(120);
                address.Property(item => item.State).HasColumnName("ShipState").HasMaxLength(2);
                address.Property(item => item.ZipCode).HasColumnName("ShipZipCode").HasMaxLength(8);
            });

            entity.HasMany(order => order.Items)
                .WithOne()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata.FindNavigation(nameof(Order.Items))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.VariantName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(item => item.LineTotal);
        });

        builder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("StockReservations");
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.Id).ValueGeneratedNever();
            entity.Ignore(reservation => reservation.IsReleased);
            entity.HasIndex(reservation => reservation.OrderId);
            entity.HasIndex(reservation => new { reservation.ProductVariantId, reservation.ReleasedAtUtc });
        });

        builder.Entity<Address>(entity =>
        {
            entity.ToTable("Addresses");
            entity.HasKey(address => address.Id);
            entity.Property(address => address.Id).ValueGeneratedNever();
            entity.Property(address => address.Label).HasMaxLength(60).IsRequired();
            entity.Property(address => address.Recipient).HasMaxLength(160).IsRequired();
            entity.Property(address => address.Street).HasMaxLength(200).IsRequired();
            entity.Property(address => address.Number).HasMaxLength(20).IsRequired();
            entity.Property(address => address.Complement).HasMaxLength(120);
            entity.Property(address => address.District).HasMaxLength(120);
            entity.Property(address => address.City).HasMaxLength(120).IsRequired();
            entity.Property(address => address.State).HasMaxLength(2).IsRequired();
            entity.Property(address => address.ZipCode).HasMaxLength(8).IsRequired();
            entity.HasIndex(address => new { address.CustomerId, address.IsDefault });
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.VariantName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(item => item.LineTotal);
            entity.HasIndex(item => new { item.CartId, item.ProductVariantId }).IsUnique();
        });
    }
}
