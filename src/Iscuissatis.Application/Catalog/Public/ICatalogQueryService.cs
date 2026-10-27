namespace Iscuissatis.Application.Catalog.Public;

/// <summary>
/// Consultas da vitrine. Somente leitura e somente produtos publicados:
/// nada de rascunho chega ao cliente.
/// </summary>
public interface ICatalogQueryService
{
    Task<PagedResult<ProductCard>> ListAsync(
        string? categorySlug = null,
        string? search = null,
        ProductSort sort = ProductSort.Relevance,
        int page = 1,
        int pageSize = 12,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryNavItem>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retorna null para rascunho, categoria inativa ou slug inexistente (a página vira 404).</summary>
    Task<ProductDetail?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
