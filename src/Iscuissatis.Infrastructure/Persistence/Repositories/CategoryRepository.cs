using Iscuissatis.Application.Catalog;
using Iscuissatis.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
        => await dbContext.Categories
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public Task<Category?> FindAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
        => dbContext.Categories.AnyAsync(
            category => category.Slug == slug && (excludeId == null || category.Id != excludeId),
            cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
