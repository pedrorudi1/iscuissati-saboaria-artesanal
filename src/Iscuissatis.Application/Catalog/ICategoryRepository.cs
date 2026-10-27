using Iscuissatis.Domain.Catalog;

namespace Iscuissatis.Application.Catalog;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    Task<Category?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    void Add(Category category);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
