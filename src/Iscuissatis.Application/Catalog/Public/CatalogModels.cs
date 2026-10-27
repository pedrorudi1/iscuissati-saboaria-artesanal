namespace Iscuissatis.Application.Catalog.Public;

public enum ProductSort
{
    Relevance = 0,
    PriceAscending = 1,
    PriceDescending = 2,
    Newest = 3,
}

public sealed record ProductCard(
    Guid Id,
    string Name,
    string Slug,
    string? ImagePath,
    string? ImageAlt,
    decimal PriceFrom,
    bool IsSoldOut,
    string CategoryName);

public sealed record ProductVariantOption(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    decimal EffectivePrice,
    int StockQuantity);

public sealed record ProductImageInfo(string Path, string AltText, int Width, int Height);

public sealed record ProductDetail(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Ingredients,
    string? HowToUse,
    string? Precautions,
    int? NetWeightGrams,
    string CategoryName,
    string CategorySlug,
    IReadOnlyList<ProductVariantOption> Variants,
    IReadOnlyList<ProductImageInfo> Images)
{
    public bool IsSoldOut => Variants.Sum(variant => variant.StockQuantity) == 0;

    public decimal PriceFrom => Variants.Count == 0 ? 0m : Variants.Min(variant => variant.EffectivePrice);
}

public sealed record CategoryNavItem(
    Guid Id,
    string Name,
    string Slug,
    int ProductCount);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
