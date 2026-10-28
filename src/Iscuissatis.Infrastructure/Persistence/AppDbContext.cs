using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Infrastructure.Notifications;
using Iscuissatis.Infrastructure.Payments;
using Iscuissatis.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<SentOrderEmail> SentOrderEmails => Set<SentOrderEmail>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FullName).HasMaxLength(160);
            user.Property(u => u.CreatedAtUtc).IsRequired();
        });

        builder.ApplyCatalog();
        builder.ApplySales();
        builder.ApplyNotifications();
        builder.ApplyPayments();
    }
}
