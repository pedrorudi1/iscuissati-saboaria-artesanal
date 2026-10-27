using Iscuissatis.Application.Catalog;
using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Iscuissatis.Application.Tests.Catalog;

public class CategoryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _repository.SlugExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _service = new CategoryService(_repository, _clock);
    }

    [Fact]
    public async Task Create_generates_slug_from_the_name()
    {
        var category = await _service.CreateAsync("Sabonetes em Barra");

        category.Slug.ShouldBe("sabonetes-em-barra");
        category.CreatedAtUtc.ShouldBe(Now);
        _repository.Received(1).Add(category);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_appends_suffix_when_the_slug_is_already_taken()
    {
        _repository.SlugExistsAsync("kits", null, Arg.Any<CancellationToken>()).Returns(true);

        var category = await _service.CreateAsync("Kits");

        category.Slug.ShouldBe("kits-2");
    }

    [Fact]
    public async Task Create_keeps_incrementing_until_it_finds_a_free_slug()
    {
        _repository.SlugExistsAsync("kits", null, Arg.Any<CancellationToken>()).Returns(true);
        _repository.SlugExistsAsync("kits-2", null, Arg.Any<CancellationToken>()).Returns(true);
        _repository.SlugExistsAsync("kits-3", null, Arg.Any<CancellationToken>()).Returns(false);

        var category = await _service.CreateAsync("Kits");

        category.Slug.ShouldBe("kits-3");
    }

    [Fact]
    public async Task Create_gives_up_after_too_many_collisions()
    {
        _repository.SlugExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Should.ThrowAsync<DomainException>(() => _service.CreateAsync("Kits"));
    }

    [Fact]
    public async Task Create_rejects_empty_name_before_touching_the_repository()
    {
        await Should.ThrowAsync<DomainException>(() => _service.CreateAsync("   "));

        _repository.DidNotReceive().Add(Arg.Any<Category>());
    }

    [Fact]
    public async Task Update_renames_without_changing_the_slug()
    {
        var category = Category.Create("Kits", Now);
        _repository.FindAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var updated = await _service.UpdateAsync(category.Id, "Kits e Presentes", "com laço", 2);

        updated.Name.ShouldBe("Kits e Presentes");
        updated.Slug.ShouldBe("kits");
        updated.SortOrder.ShouldBe(2);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_throws_when_the_category_does_not_exist()
    {
        _repository.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        await Should.ThrowAsync<DomainException>(() => _service.UpdateAsync(Guid.NewGuid(), "Kits", null, 0));
    }

    [Fact]
    public async Task SetActive_deactivates_and_records_the_current_time()
    {
        var category = Category.Create("Kits", Now);
        _repository.FindAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _clock.Advance(TimeSpan.FromHours(2));

        await _service.SetActiveAsync(category.Id, false);

        category.IsActive.ShouldBeFalse();
        category.UpdatedAtUtc.ShouldBe(Now.AddHours(2));
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_returns_repository_order_untouched()
    {
        var categories = new List<Category>
        {
            Category.Create("Kits", Now, sortOrder: 2),
            Category.Create("Sabonetes", Now, sortOrder: 1),
        };
        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(categories);

        var result = await _service.ListAsync();

        result.ShouldBe(categories);
    }
}
