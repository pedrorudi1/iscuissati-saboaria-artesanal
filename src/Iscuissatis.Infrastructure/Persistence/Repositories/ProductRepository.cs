using Iscuissatis.Application.Catalog;
using Iscuissatis.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(AppDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> ListAsync(
        string? search = null,
        ProductStatus? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(product =>
                EF.Functions.ILike(product.Name, term)
                || product.Variants.Any(variant => EF.Functions.ILike(variant.Sku, term)));
        }

        if (status is not null)
        {
            query = query.Where(product => product.Status == status);
        }

        if (categoryId is not null)
        {
            query = query.Where(product => product.CategoryId == categoryId);
        }

        return await query
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Products
            .Include(product => product.Variants)
            .Include(product => product.Images)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
        => dbContext.Products.AnyAsync(
            product => product.Slug == slug && (excludeId == null || product.Id != excludeId),
            cancellationToken);

    public Task<bool> SkuExistsAsync(
        string sku,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default)
        => dbContext.ProductVariants.AnyAsync(
            variant => variant.Sku == sku
                       && (excludeProductId == null || variant.ProductId != excludeProductId),
            cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
