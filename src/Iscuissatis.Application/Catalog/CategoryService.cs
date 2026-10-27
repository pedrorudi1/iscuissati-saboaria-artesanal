using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;

namespace Iscuissatis.Application.Catalog;

/// <summary>
/// Casos de uso de categorias. A regra de unicidade do slug vive aqui porque
/// depende do repositório; a validação do nome continua no domínio.
/// </summary>
public sealed class CategoryService(ICategoryRepository repository, TimeProvider clock)
{
    private const int MaxSlugAttempts = 50;

    public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
        => repository.ListAsync(cancellationToken);

    public async Task<Category> CreateAsync(
        string name,
        string? description = null,
        int sortOrder = 0,
        CancellationToken cancellationToken = default)
    {
        var slug = await BuildUniqueSlugAsync(name, excludeId: null, cancellationToken);

        var category = Category.Create(name, clock.GetUtcNow(), description, sortOrder, slug);

        repository.Add(category);
        await repository.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<Category> UpdateAsync(
        Guid id,
        string name,
        string? description = null,
        int sortOrder = 0,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.FindAsync(id, cancellationToken)
            ?? throw new DomainException("Categoria não encontrada.");

        category.Rename(name, clock.GetUtcNow());
        category.Reorder(sortOrder, clock.GetUtcNow());
        category.SetDescription(description, clock.GetUtcNow());

        await repository.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.FindAsync(id, cancellationToken)
            ?? throw new DomainException("Categoria não encontrada.");

        var now = clock.GetUtcNow();

        if (active)
        {
            category.Activate(now);
        }
        else
        {
            category.Deactivate(now);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> BuildUniqueSlugAsync(
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new DomainException("O nome da categoria é obrigatório.");
        }

        var baseSlug = Slug.Create(trimmed).Value;

        if (!await repository.SlugExistsAsync(baseSlug, excludeId, cancellationToken))
        {
            return baseSlug;
        }

        for (var attempt = 2; attempt <= MaxSlugAttempts; attempt++)
        {
            var candidate = $"{baseSlug}-{attempt}";

            if (!await repository.SlugExistsAsync(candidate, excludeId, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainException(
            $"Não foi possível gerar um endereço único para '{trimmed}'. Ajuste o nome da categoria.");
    }
}
