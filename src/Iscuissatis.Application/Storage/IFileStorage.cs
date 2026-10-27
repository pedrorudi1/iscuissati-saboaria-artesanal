namespace Iscuissatis.Application.Storage;

/// <summary>
/// Armazenamento de arquivos públicos (imagens de produto).
/// A implementação inicial grava em disco/volume; trocar por S3/R2 não muda o domínio.
/// </summary>
public interface IFileStorage
{
    /// <summary>Grava o conteúdo e devolve o caminho público (ex.: "/media/produtos/abc.webp").</summary>
    Task<string> SaveAsync(
        ReadOnlyMemory<byte> content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string publicPath, CancellationToken cancellationToken = default);
}
