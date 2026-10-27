namespace Iscuissatis.Application.Catalog;

/// <summary>
/// Dados mínimos de uma variação para validar o carrinho sem carregar o produto inteiro.
/// </summary>
public sealed record VariantSnapshot(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    bool IsPublished);
