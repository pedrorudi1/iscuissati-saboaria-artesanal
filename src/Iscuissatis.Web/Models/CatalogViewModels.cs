using Iscuissatis.Application.Catalog.Public;

namespace Iscuissatis.Web.Models;

public class CatalogListViewModel
{
    public PagedResult<ProductCard> Result { get; set; } = new([], 1, 12, 0);

    public IReadOnlyList<CategoryNavItem> Categories { get; set; } = [];

    public string? CategorySlug { get; set; }

    public string? Search { get; set; }

    public ProductSort Sort { get; set; } = ProductSort.Relevance;

    public string? CurrentCategoryName
        => Categories.FirstOrDefault(item => item.Slug == CategorySlug)?.Name;
}

public class HomeViewModel
{
    public IReadOnlyList<CategoryNavItem> Categories { get; set; } = [];

    public IReadOnlyList<ProductCard> Featured { get; set; } = [];

    public int PublishedCount { get; set; }
}
