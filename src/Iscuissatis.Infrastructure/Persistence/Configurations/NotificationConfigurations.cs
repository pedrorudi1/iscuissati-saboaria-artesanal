using Iscuissatis.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Configurations;

public static class NotificationConfigurations
{
    public static void ApplyNotifications(this ModelBuilder builder)
    {
        builder.Entity<SentOrderEmail>(entity =>
        {
            entity.ToTable("SentOrderEmails");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.To).HasMaxLength(180).IsRequired();
            entity.HasIndex(item => new { item.OrderId, item.Kind }).IsUnique();
        });
    }
}
