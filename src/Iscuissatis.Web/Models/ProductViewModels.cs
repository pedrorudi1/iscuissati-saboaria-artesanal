using Iscuissatis.Domain.Catalog;

namespace Iscuissatis.Web.Models;

public class ProductListViewModel
{
    public IReadOnlyList<Product> Products { get; set; } = [];

    public IReadOnlyList<Category> Categories { get; set; } = [];

    public string? Search { get; set; }

    public ProductStatus? Status { get; set; }

    public Guid? CategoryId { get; set; }
}

public class ProductFormViewModel
{
    public ProductFormModel Form { get; set; } = new();

    public VariantFormModel NewVariant { get; set; } = new();

    public IReadOnlyList<Category> Categories { get; set; } = [];

    public IReadOnlyList<ProductVariant> Variants { get; set; } = [];

    public int ImageCount { get; set; }

    public bool IsPublished { get; set; }

    public bool IsEdit => Form.Id != Guid.Empty;
}
