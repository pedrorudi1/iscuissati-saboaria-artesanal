using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Catalog;

public sealed class Product
{
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductImage> _images = [];

    private Product()
    {
    }

    private Product(string name, string slug, Guid categoryId, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        CategoryId = categoryId;
        Status = ProductStatus.Draft;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Imutável após a criação: mudar o nome não quebra links já indexados.</summary>
    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>Ingredientes na nomenclatura INCI, exigida na rotulagem de cosméticos.</summary>
    public string? Ingredients { get; private set; }

    public string? HowToUse { get; private set; }

    public string? Precautions { get; private set; }

    /// <summary>Peso líquido em gramas, exibido na página do produto.</summary>
    public int? NetWeightGrams { get; private set; }

    public ProductStatus Status { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public Guid CategoryId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants;

    public IReadOnlyCollection<ProductImage> Images => _images;

    public bool IsPublished => Status == ProductStatus.Published;

    public bool IsSoldOut => _variants.Where(variant => variant.IsActive).Sum(variant => variant.StockQuantity) == 0;

    public static Product Create(
        string name,
        Guid categoryId,
        DateTimeOffset now,
        string? description = null,
        string? ingredients = null,
        string? howToUse = null,
        string? precautions = null,
        int? netWeightGrams = null,
        string? slug = null)
    {
        var trimmed = ValidateName(name);

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("O produto precisa de uma categoria.");
        }

        if (netWeightGrams is < 0)
        {
            throw new DomainException("O peso líquido não pode ser negativo.");
        }

        var finalSlug = string.IsNullOrWhiteSpace(slug)
            ? Iscuissatis.Domain.Common.Slug.Create(trimmed).Value
            : Iscuissatis.Domain.Common.Slug.Create(slug.Trim()).Value;

        return new Product(trimmed, finalSlug, categoryId, now)
        {
            Description = description,
            Ingredients = ingredients,
            HowToUse = howToUse,
            Precautions = precautions,
            NetWeightGrams = netWeightGrams,
        };
    }

    public void UpdateDetails(
        string name,
        Guid categoryId,
        DateTimeOffset now,
        string? description = null,
        string? ingredients = null,
        string? howToUse = null,
        string? precautions = null,
        int? netWeightGrams = null,
        string? slug = null)
    {
        var trimmed = ValidateName(name);

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("O produto precisa de uma categoria.");
        }

        if (netWeightGrams is < 0)
        {
            throw new DomainException("O peso líquido não pode ser negativo.");
        }

        Name = trimmed;
        CategoryId = categoryId;
        Description = description;
        Ingredients = ingredients;
        HowToUse = howToUse;
        Precautions = precautions;
        NetWeightGrams = netWeightGrams;
        UpdatedAtUtc = now;
    }

    public ProductVariant AddVariant(
        string name,
        string sku,
        decimal price,
        int stockQuantity,
        int weightGrams,
        DateTimeOffset now,
        decimal lengthCm = 0,
        decimal widthCm = 0,
        decimal heightCm = 0,
        decimal? promotionalPrice = null)
    {
        var normalizedSku = (sku ?? string.Empty).Trim().ToUpperInvariant();

        if (_variants.Any(variant => variant.Sku == normalizedSku))
        {
            throw new DomainException($"Já existe uma variação com o SKU {normalizedSku} neste produto.");
        }

        var variant = ProductVariant.Create(
            Id, name, normalizedSku, price, stockQuantity, weightGrams,
            lengthCm, widthCm, heightCm, promotionalPrice, now);

        _variants.Add(variant);
        UpdatedAtUtc = now;

        return variant;
    }

    public ProductImage AddImage(string path, string altText, DateTimeOffset now)
    {
        var image = ProductImage.Create(
            Id, path, altText, _images.Count, isPrimary: _images.Count == 0, now);

        _images.Add(image);
        UpdatedAtUtc = now;

        return image;
    }

    /// <summary>
    /// Publica o produto. Exige ao menos uma variação ativa e uma imagem:
    /// produto publicado sem foto ou sem como comprar só gera frustração.
    /// </summary>
    public void Publish(DateTimeOffset now)
    {
        if (!_variants.Any(variant => variant.IsActive))
        {
            throw new DomainException("Para publicar, cadastre ao menos uma variação ativa.");
        }

        if (_images.Count == 0)
        {
            throw new DomainException("Para publicar, adicione ao menos uma imagem.");
        }

        Status = ProductStatus.Published;
        PublishedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Unpublish(DateTimeOffset now)
    {
        Status = ProductStatus.Draft;
        PublishedAtUtc = null;
        UpdatedAtUtc = now;
    }

    private static string ValidateName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new DomainException("O nome do produto é obrigatório.");
        }

        return trimmed;
    }
}
