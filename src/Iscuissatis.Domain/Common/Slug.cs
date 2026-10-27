using System.Text;

namespace Iscuissatis.Domain.Common;

/// <summary>
/// Identificador legível usado em URLs de produtos, categorias e páginas de conteúdo.
/// Normaliza o texto removendo acentos, pontuação e espaços repetidos.
/// </summary>
public sealed record Slug
{
    public const int MaxLength = 120;

    private static readonly Dictionary<char, char> Diacritics = new()
    {
        ['á'] = 'a', ['à'] = 'a', ['â'] = 'a', ['ã'] = 'a', ['ä'] = 'a', ['å'] = 'a',
        ['é'] = 'e', ['è'] = 'e', ['ê'] = 'e', ['ë'] = 'e',
        ['í'] = 'i', ['ì'] = 'i', ['î'] = 'i', ['ï'] = 'i',
        ['ó'] = 'o', ['ò'] = 'o', ['ô'] = 'o', ['õ'] = 'o', ['ö'] = 'o',
        ['ú'] = 'u', ['ù'] = 'u', ['û'] = 'u', ['ü'] = 'u',
        ['ç'] = 'c', ['ñ'] = 'n', ['ý'] = 'y', ['ÿ'] = 'y',
    };

    private Slug(string value) => Value = value;

    public string Value { get; }

    public static Slug Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Slug não pode ser vazio.", nameof(text));
        }

        var builder = new StringBuilder(text.Length);
        var previousWasSeparator = true;

        foreach (var character in text.ToLowerInvariant())
        {
            var normalized = Diacritics.GetValueOrDefault(character, character);

            if (char.IsLetterOrDigit(normalized))
            {
                builder.Append(normalized);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var value = builder.ToString().Trim('-');

        if (value.Length == 0)
        {
            throw new ArgumentException("Slug precisa conter ao menos uma letra ou número.", nameof(text));
        }

        if (value.Length > MaxLength)
        {
            value = value[..MaxLength].Trim('-');
        }

        return new Slug(value);
    }

    public override string ToString() => Value;
}
