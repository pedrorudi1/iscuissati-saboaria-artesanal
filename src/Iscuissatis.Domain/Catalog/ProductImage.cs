using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Catalog;

public sealed class ProductImage
{
    private ProductImage()
    {
    }

    private ProductImage(
        Guid productId,
        string path,
        string altText,
        int sortOrder,
        bool isPrimary,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Path = path;
        AltText = altText;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        CreatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    /// <summary>Caminho relativo do arquivo já processado (WebP).</summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>Texto alternativo: obrigatório por acessibilidade e SEO.</summary>
    public string AltText { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ProductImage Create(
        Guid productId,
        string path,
        string altText,
        int sortOrder,
        bool isPrimary,
        DateTimeOffset now)
    {
        var trimmedPath = (path ?? string.Empty).Trim();
        var trimmedAlt = (altText ?? string.Empty).Trim();

        if (trimmedPath.Length == 0)
        {
            throw new DomainException("O caminho da imagem é obrigatório.");
        }

        if (trimmedAlt.Length == 0)
        {
            throw new DomainException("O texto alternativo da imagem é obrigatório.");
        }

        if (sortOrder < 0)
        {
            throw new DomainException("A ordem da imagem não pode ser negativa.");
        }

        return new ProductImage(productId, trimmedPath, trimmedAlt, sortOrder, isPrimary, now);
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void UnsetPrimary()
    {
        IsPrimary = false;
    }
}
