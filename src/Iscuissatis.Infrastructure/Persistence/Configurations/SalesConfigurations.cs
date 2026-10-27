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
