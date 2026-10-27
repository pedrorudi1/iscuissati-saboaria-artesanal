using Iscuissatis.Domain.Catalog;
using Iscuissatis.Domain.Common;
using Shouldly;

namespace Iscuissatis.Domain.Tests.Catalog;

public class ProductImageTests
{
    [Fact]
    public void Create_requires_a_path()
    {
        Should.Throw<DomainException>(() =>
            ProductImage.Create(Guid.NewGuid(), "  ", "Sabonete", 0, true, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_requires_alt_text_for_accessibility()
    {
        Should.Throw<DomainException>(() =>
            ProductImage.Create(Guid.NewGuid(), "/img/a.webp", "", 0, true, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_accepts_a_valid_image()
    {
        var image = ProductImage.Create(
            Guid.NewGuid(), "/img/a.webp", "Sabonete de lavanda sobre mesa de madeira",
            1, false, DateTimeOffset.UtcNow);

        image.Path.ShouldBe("/img/a.webp");
        image.IsPrimary.ShouldBeFalse();
        image.SortOrder.ShouldBe(1);
    }
}
