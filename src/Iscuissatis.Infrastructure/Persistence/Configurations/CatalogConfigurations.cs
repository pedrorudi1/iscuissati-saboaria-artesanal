using Iscuissatis.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Iscuissatis.Infrastructure.Persistence.Configurations;

public static class CatalogConfigurations
{
    public static void ApplyCatalog(this ModelBuilder builder)
    {
        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).ValueGeneratedNever();
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Slug).HasMaxLength(140).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(600);
            entity.HasIndex(category => category.Slug).IsUnique();
            entity.HasIndex(category => new { category.IsActive, category.SortOrder });
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).ValueGeneratedNever();
            entity.Property(product => product.Name).HasMaxLength(160).IsRequired();
            entity.Property(product => product.Slug).HasMaxLength(180).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(4000);
            entity.Property(product => product.Ingredients).HasMaxLength(2000);
            entity.Property(product => product.HowToUse).HasMaxLength(2000);
            entity.Property(product => product.Precautions).HasMaxLength(2000);
            entity.HasIndex(product => product.Slug).IsUnique();
            entity.HasIndex(product => new { product.CategoryId, product.Status });

            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(product => product.Variants)
                .WithOne()
                .HasForeignKey(variant => variant.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(product => product.Images)
                .WithOne()
                .HasForeignKey(image => image.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata.FindNavigation(nameof(Product.Variants))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(Product.Images))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");
            entity.HasKey(variant => variant.Id);
            entity.Property(variant => variant.Id).ValueGeneratedNever();
            entity.Property(variant => variant.Name).HasMaxLength(120).IsRequired();
            entity.Property(variant => variant.Sku).HasMaxLength(60).IsRequired();
            entity.Property(variant => variant.Price).HasPrecision(18, 2);
            entity.Property(variant => variant.PromotionalPrice).HasPrecision(18, 2);
            entity.Property(variant => variant.LengthCm).HasPrecision(10, 2);
            entity.Property(variant => variant.WidthCm).HasPrecision(10, 2);
            entity.Property(variant => variant.HeightCm).HasPrecision(10, 2);
            entity.Ignore(variant => variant.EffectivePrice);
            entity.HasIndex(variant => variant.Sku).IsUnique();
            entity.HasIndex(variant => new { variant.ProductId, variant.IsActive });
        });

        builder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.HasKey(image => image.Id);
            entity.Property(image => image.Id).ValueGeneratedNever();
            entity.Property(image => image.Path).HasMaxLength(300).IsRequired();
            entity.Property(image => image.AltText).HasMaxLength(200).IsRequired();
            entity.HasIndex(image => new { image.ProductId, image.SortOrder });
        });
    }
}
