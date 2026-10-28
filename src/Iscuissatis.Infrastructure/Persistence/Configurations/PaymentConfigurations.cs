using Iscuissatis.Domain.Sales;
using Iscuissatis.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Configurations;

public static class PaymentConfigurations
{
    public static void ApplyPayments(this ModelBuilder builder)
    {
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Id).ValueGeneratedNever();
            entity.Property(payment => payment.Provider).HasMaxLength(40).IsRequired();
            entity.Property(payment => payment.ProviderPaymentId).HasMaxLength(120).IsRequired();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.CopyPaste).HasMaxLength(2000);
            entity.HasIndex(payment => new { payment.Provider, payment.ProviderPaymentId }).IsUnique();
            entity.HasIndex(payment => payment.OrderId);
        });

        builder.Entity<ProcessedWebhookEvent>(entity =>
        {
            entity.ToTable("ProcessedWebhookEvents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Provider).HasMaxLength(40).IsRequired();
            entity.Property(item => item.EventId).HasMaxLength(120).IsRequired();
            entity.HasIndex(item => new { item.Provider, item.EventId }).IsUnique();
        });
    }
}
