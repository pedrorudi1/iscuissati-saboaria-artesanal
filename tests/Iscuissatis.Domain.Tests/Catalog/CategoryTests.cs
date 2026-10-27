using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Shouldly;

namespace Iscuissatis.Domain.Tests.Catalog;

public class CategoryTests
{
    [Fact]
    public void Create_generates_slug_from_name()
    {
        var category = Category.Create("Sabonetes em Barra", DateTimeOffset.UtcNow);

        category.Slug.ShouldBe("sabonetes-em-barra");
        category.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_empty_name()
    {
        Should.Throw<DomainException>(() => Category.Create("  ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_rejects_negative_sort_order()
    {
        Should.Throw<DomainException>(() => Category.Create("Kits", DateTimeOffset.UtcNow, sortOrder: -1));
    }

    [Fact]
    public void Rename_keeps_the_slug_stable()
    {
        var category = Category.Create("Kits", DateTimeOffset.UtcNow);

        category.Rename("Kits e Presentes", DateTimeOffset.UtcNow);

        category.Name.ShouldBe("Kits e Presentes");
        category.Slug.ShouldBe("kits");
    }

    [Fact]
    public void Deactivate_and_activate_toggle_visibility()
    {
        var category = Category.Create("Kits", DateTimeOffset.UtcNow);

        category.Deactivate(DateTimeOffset.UtcNow);
        category.IsActive.ShouldBeFalse();

        category.Activate(DateTimeOffset.UtcNow);
        category.IsActive.ShouldBeTrue();
    }
}
