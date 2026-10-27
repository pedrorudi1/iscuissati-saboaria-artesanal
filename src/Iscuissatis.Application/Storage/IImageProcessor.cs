namespace Iscuissatis.Application.Storage;

/// <summary>
/// Valida o arquivo pelo conteúdo (não pela extensão) e devolve uma versão
/// padronizada em WebP, com as dimensões finais.
/// </summary>
public interface IImageProcessor
{
    /// <summary>Retorna null quando o conteúdo não é uma imagem suportada.</summary>
    Task<ProcessedImage?> ProcessAsync(Stream content, CancellationToken cancellationToken = default);
}

public sealed record ProcessedImage(byte[] Content, string ContentType, int Width, int Height)
{
    public string Extension => ContentType switch
    {
        "image/webp" => ".webp",
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        _ => ".bin",
    };
}
