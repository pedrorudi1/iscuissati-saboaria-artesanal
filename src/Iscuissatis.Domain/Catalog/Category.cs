using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Catalog;

public sealed class Category
{
    private Category()
    {
    }

    private Category(string name, string slug, string? description, int sortOrder, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        Description = description;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Imutável após a criação: mudar o nome não quebra links já publicados.</summary>
    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Category Create(
        string name,
        DateTimeOffset now,
        string? description = null,
        int sortOrder = 0)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new DomainException("O nome da categoria é obrigatório.");
        }

        if (sortOrder < 0)
        {
            throw new DomainException("A ordem da categoria não pode ser negativa.");
        }

        return new Category(trimmed, Iscuissatis.Domain.Common.Slug.Create(trimmed).Value, description, sortOrder, now);
    }

    public void Rename(string name, DateTimeOffset now)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new DomainException("O nome da categoria é obrigatório.");
        }

        Name = trimmed;
        UpdatedAtUtc = now;
    }

    public void Reorder(int sortOrder, DateTimeOffset now)
    {
        if (sortOrder < 0)
        {
            throw new DomainException("A ordem da categoria não pode ser negativa.");
        }

        SortOrder = sortOrder;
        UpdatedAtUtc = now;
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }
}
