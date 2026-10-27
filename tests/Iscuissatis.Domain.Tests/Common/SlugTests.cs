using Iscuissatis.Domain.Common;
using Shouldly;

namespace Iscuissatis.Domain.Tests.Common;

public class SlugTests
{
    [Theory]
    [InlineData("Sabonete de Lavanda", "sabonete-de-lavanda")]
    [InlineData("  Sabonete  de   Lavanda  ", "sabonete-de-lavanda")]
    [InlineData("Sabonete de Lavanda ", "sabonete-de-lavanda")]
    [InlineData("AÇÃO & Hidratação", "acao-hidratacao")]
    [InlineData("Sabonete 100% Natural", "sabonete-100-natural")]
    [InlineData("Sabonete Barra Terracota", "sabonete-barra-terracota")]
    public void Create_normalizes_text_to_url_friendly_slug(string input, string expected)
    {
        Slug.Create(input).Value.ShouldBe(expected);
    }

    [Fact]
    public void Create_is_case_and_accent_insensitive()
    {
        Slug.Create("AÇÃO").ShouldBe(Slug.Create("acao"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void Create_rejects_text_without_any_letter_or_digit(string input)
    {
        Should.Throw<ArgumentException>(() => Slug.Create(input));
    }

    [Fact]
    public void Two_slugs_created_from_the_same_text_are_equal()
    {
        var first = Slug.Create("Sabonete de Lavanda");
        var second = Slug.Create("Sabonete de Lavanda");

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }
}
