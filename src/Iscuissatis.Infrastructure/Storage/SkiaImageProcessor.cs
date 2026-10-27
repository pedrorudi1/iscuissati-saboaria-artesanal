using Iscuissatis.Application.Storage;
using Iscuissatis.Domain.Common;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Iscuissatis.Infrastructure.Storage;

/// <summary>
/// Decodifica o arquivo com SkiaSharp (licença MIT) para validar pelo CONTEÚDO,
/// redimensiona até 1600 px de largura e reencoda em WebP.
/// Arquivo que não é imagem decodificável retorna null e é recusado pelo serviço.
/// </summary>
public sealed class SkiaImageProcessor(IOptions<StorageOptions> options) : IImageProcessor
{
    private const int MaxWidth = 1600;
    private const int WebpQuality = 82;

    private readonly long _maxBytes = options.Value.MaxUploadBytes;

    public async Task<ProcessedImage?> ProcessAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
        {
            return null;
        }

        if (buffer.Length > _maxBytes)
        {
            throw new DomainException(
                $"A imagem deve ter no máximo {_maxBytes / (1024 * 1024)} MB.");
        }

        buffer.Position = 0;

        using var original = SKBitmap.Decode(buffer);

        if (original is null || original.Width <= 0 || original.Height <= 0)
        {
            return null;
        }

        var scale = original.Width > MaxWidth ? (float)MaxWidth / original.Width : 1f;
        var width = Math.Max(1, (int)Math.Round(original.Width * scale));
        var height = Math.Max(1, (int)Math.Round(original.Height * scale));

        using var image = SKImage.FromBitmap(original);
        using var surface = SKSurface.Create(new SKImageInfo(width, height));

        surface.Canvas.DrawImage(
            image,
            new SKRect(0, 0, width, height),
            new SKSamplingOptions(SKCubicResampler.Mitchell));

        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Webp, WebpQuality);

        if (encoded is null)
        {
            return null;
        }

        return new ProcessedImage(encoded.ToArray(), "image/webp", width, height);
    }
}
