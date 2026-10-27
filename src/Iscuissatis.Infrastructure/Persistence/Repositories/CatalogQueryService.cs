using Iscuissatis.Application.Catalog.Public;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Repositories;

/// <summary>
/// Consultas da vitrine direto no banco (sem tracking). Projeção enxuta:
/// a home e a listagem não carregam entidades inteiras.
/// </summary>
public sealed class CatalogQueryService(AppDbContext dbContext) : ICatalogQueryService
{
    public async Task<PagedResult<ProductCard>> ListAsync(
        string? categorySlug = null,
        string? search = null,
        ProductSort sort = ProductSort.Relevance,
        int page = 1,
        int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 48);

        var activeCategories = dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive);

        var query = dbContext.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Published)
            .Join(
                activeCategories,
                product => product.CategoryId,
                category => category.Id,
                (product, category) => new { Product = product, Category = category });

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(item => item.Category.Slug == categorySlug);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = Slug.Create(search.Trim()).Value;
            query = query.Where(item => EF.Functions.ILike(item.Product.SearchTerms, $"%{normalized}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        query = sort switch
        {
            ProductSort.PriceAscending => query.OrderBy(item => item.Product.Variants
                .Where(variant => variant.IsActive)
                .Min(variant => (decimal?)(variant.PromotionalPrice ?? variant.Price)) ?? decimal.MaxValue),

            ProductSort.PriceDescending => query.OrderByDescending(item => item.Product.Variants
                .Where(variant => variant.IsActive)
                .Max(variant => (decimal?)(variant.PromotionalPrice ?? variant.Price)) ?? 0),

            ProductSort.Newest => query.OrderByDescending(item =>
                item.Product.PublishedAtUtc ?? item.Product.CreatedAtUtc),

            _ => query.OrderBy(item => item.Product.Name),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ProductCard(
                item.Product.Id,
                item.Product.Name,
                item.Product.Slug,
                item.Product.Images
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.SortOrder)
                    .Select(image => image.Path)
                    .FirstOrDefault(),
                item.Product.Images
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.SortOrder)
                    .Select(image => image.AltText)
                    .FirstOrDefault(),
                item.Product.Variants
                    .Where(variant => variant.IsActive)
                    .Min(variant => (decimal?)(variant.PromotionalPrice ?? variant.Price)) ?? 0m,
                item.Product.Variants
                    .Where(variant => variant.IsActive)
                    .Sum(variant => variant.StockQuantity) == 0,
                item.Category.Name))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductCard>(items, page, pageSize, total);
    }

    public async Task<ProductDetail?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.Variants)
            .Include(item => item.Images)
            .FirstOrDefaultAsync(
                item => item.Slug == slug && item.Status == ProductStatus.Published,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == product.CategoryId && item.IsActive,
                cancellationToken);

        if (category is null)
        {
            return null;
        }

        return new ProductDetail(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Ingredients,
            product.HowToUse,
            product.Precautions,
            product.NetWeightGrams,
            category.Name,
            category.Slug,
            product.Variants
                .Where(variant => variant.IsActive)
                .OrderBy(variant => variant.EffectivePrice)
                .Select(variant => new ProductVariantOption(
                    variant.Id, variant.Name, variant.Sku,
                    variant.Price, variant.EffectivePrice, variant.StockQuantity))
                .ToList(),
            product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder)
                .Select(image => new ProductImageInfo(image.Path, image.AltText, image.Width, image.Height))
                .ToList());
    }

    public async Task<IReadOnlyList<CategoryNavItem>> ListCategoriesAsync(
        CancellationToken cancellationToken = default)
        => await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryNavItem(
                category.Id,
                category.Name,
                category.Slug,
                dbContext.Products.Count(product =>
                    product.CategoryId == category.Id && product.Status == ProductStatus.Published)))
            .ToListAsync(cancellationToken);
}
