namespace Iscuissatis.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Pasta física onde as imagens são gravadas. No container é um volume
    /// (ex.: /app/media); no Coolify, monte um volume persistente aqui.
    /// </summary>
    public string RootPath { get; set; } = "wwwroot/media";

    /// <summary>Caminho público servido pela aplicação.</summary>
    public string PublicPrefix { get; set; } = "/media";

    /// <summary>Tamanho máximo aceito para upload, em bytes.</summary>
    public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;
}
