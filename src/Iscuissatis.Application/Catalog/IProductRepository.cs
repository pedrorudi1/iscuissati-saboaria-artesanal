using Iscuissatis.Domain.Catalog;

namespace Iscuissatis.Application.Catalog;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListAsync(
        string? search = null,
        ProductStatus? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(string sku, Guid? excludeProductId = null, CancellationToken cancellationToken = default);

    Task<VariantSnapshot?> GetVariantSnapshotAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    void Add(Product product);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
