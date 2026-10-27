using Iscuissatis.Application.Storage;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;

namespace Iscuissatis.Application.Catalog;

/// <summary>
/// Casos de uso de produtos. Unicidade de slug e de SKU dependem do repositório;
/// as demais regras (publicação, estoque, preço) ficam no domínio.
/// </summary>
public sealed class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    TimeProvider clock,
    IFileStorage storage,
    IImageProcessor imageProcessor)
{
    private const int MaxSlugAttempts = 50;

    public Task<IReadOnlyList<Product>> ListAsync(
        string? search = null,
        ProductStatus? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
        => products.ListAsync(search, status, categoryId, cancellationToken);

    public async Task<Product> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await products.FindAsync(id, cancellationToken)
           ?? throw new DomainException("Produto não encontrado.");

    public async Task<Product> CreateAsync(
        Guid categoryId,
        string name,
        string? description = null,
        string? ingredients = null,
        string? howToUse = null,
        string? precautions = null,
        int? netWeightGrams = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureCategoryAsync(categoryId, cancellationToken);
        var slug = await BuildUniqueSlugAsync(name, excludeId: null, cancellationToken);

        var product = Product.Create(
            name, categoryId, clock.GetUtcNow(),
            description, ingredients, howToUse, precautions, netWeightGrams, slug);

        products.Add(product);
        await products.SaveChangesAsync(cancellationToken);

        return product;
    }

    public async Task<Product> UpdateAsync(
        Guid id,
        Guid categoryId,
        string name,
        string? description = null,
        string? ingredients = null,
        string? howToUse = null,
        string? precautions = null,
        int? netWeightGrams = null,
        CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(id, cancellationToken);
        await EnsureCategoryAsync(categoryId, cancellationToken);

        product.UpdateDetails(
            name, categoryId, clock.GetUtcNow(),
            description, ingredients, howToUse, precautions, netWeightGrams);

        await products.SaveChangesAsync(cancellationToken);

        return product;
    }

    public async Task<ProductVariant> AddVariantAsync(
        Guid productId,
        string name,
        string sku,
        decimal price,
        int stockQuantity,
        int weightGrams,
        decimal lengthCm = 0,
        decimal widthCm = 0,
        decimal heightCm = 0,
        decimal? promotionalPrice = null,
        CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(productId, cancellationToken);
        var normalizedSku = (sku ?? string.Empty).Trim().ToUpperInvariant();

        if (await products.SkuExistsAsync(normalizedSku, excludeProductId: null, cancellationToken))
        {
            throw new DomainException($"O SKU {normalizedSku} já está em uso por outro produto.");
        }

        var variant = product.AddVariant(
            name, normalizedSku, price, stockQuantity, weightGrams,
            clock.GetUtcNow(), lengthCm, widthCm, heightCm, promotionalPrice);

        await products.SaveChangesAsync(cancellationToken);

        return variant;
    }

    public async Task AdjustStockAsync(
        Guid productId,
        Guid variantId,
        int delta,
        CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(productId, cancellationToken);
        var variant = product.Variants.FirstOrDefault(item => item.Id == variantId)
            ?? throw new DomainException("Variação não encontrada.");

        variant.AdjustStock(delta, clock.GetUtcNow());
        await products.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductImage> AddImageAsync(
        Guid productId,
        Stream content,
        string fileName,
        string altText,
        CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(productId, cancellationToken);

        var processed = await imageProcessor.ProcessAsync(content, cancellationToken)
            ?? throw new DomainException("O arquivo enviado não é uma imagem válida (use JPEG, PNG ou WebP).");

        var path = await storage.SaveAsync(
            processed.Content,
            Path.ChangeExtension(fileName, processed.Extension),
            processed.ContentType,
            cancellationToken);

        var image = product.AddImage(path, altText, clock.GetUtcNow(), processed.Width, processed.Height);

        await products.SaveChangesAsync(cancellationToken);

        return image;
    }

    public async Task RemoveImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(productId, cancellationToken);
        var image = product.RemoveImage(imageId, clock.GetUtcNow());

        await products.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(image.Path, cancellationToken);
    }

    public async Task SetPrimaryImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(productId, cancellationToken);
        product.SetPrimaryImage(imageId, clock.GetUtcNow());

        await products.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(id, cancellationToken);
        product.Publish(clock.GetUtcNow());
        await products.SaveChangesAsync(cancellationToken);
    }

    public async Task UnpublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetAsync(id, cancellationToken);
        product.Unpublish(clock.GetUtcNow());
        await products.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Escolha uma categoria para o produto.");
        }

        if (await categories.FindAsync(categoryId, cancellationToken) is null)
        {
            throw new DomainException("Categoria não encontrada. Escolha uma categoria válida.");
        }
    }

    private async Task<string> BuildUniqueSlugAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new DomainException("O nome do produto é obrigatório.");
        }

        var baseSlug = Slug.Create(trimmed).Value;

        if (!await products.SlugExistsAsync(baseSlug, excludeId, cancellationToken))
        {
            return baseSlug;
        }

        for (var attempt = 2; attempt <= MaxSlugAttempts; attempt++)
        {
            var candidate = $"{baseSlug}-{attempt}";

            if (!await products.SlugExistsAsync(candidate, excludeId, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException(
            $"Não foi possível gerar um endereço único para '{trimmed}'. Ajuste o nome do produto.");
    }
}
