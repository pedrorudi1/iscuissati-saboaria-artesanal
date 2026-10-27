using Iscuissatis.Application.Storage;
using Microsoft.Extensions.Options;

namespace Iscuissatis.Infrastructure.Storage;

/// <summary>
/// Grava em disco, em pasta por tipo de conteúdo e por mês, com nome aleatório.
/// O caminho público é servido por <c>/media</c> (static files da própria app).
/// </summary>
public sealed class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> SaveAsync(
        ReadOnlyMemory<byte> content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(
            _options.RootPath,
            "produtos",
            DateTime.UtcNow.ToString("yyyy"),
            DateTime.UtcNow.ToString("MM"));

        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(fileName);
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, safeName);

        await File.WriteAllBytesAsync(fullPath, content.ToArray(), cancellationToken);

        var relative = Path.Combine("produtos", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), safeName)
            .Replace(Path.DirectorySeparatorChar, '/');

        return $"{_options.PublicPrefix.TrimEnd('/')}/{relative}";
    }

    public Task DeleteAsync(string publicPath, CancellationToken cancellationToken = default)
    {
        if (!publicPath.StartsWith(_options.PublicPrefix, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        var relative = publicPath[_options.PublicPrefix.Length..].TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, relative));
        var root = Path.GetFullPath(_options.RootPath);

        // Nunca apaga fora da raiz configurada.
        if (!fullPath.StartsWith(root, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
