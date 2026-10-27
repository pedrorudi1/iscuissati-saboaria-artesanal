namespace Iscuissatis.Application.Shipping;

public sealed record ShippingQuoteRequest(string ZipCode, int TotalWeightGrams, decimal Subtotal);

public sealed record ShippingOption(
    string Code,
    string Name,
    decimal Price,
    int EstimatedBusinessDays,
    bool IsPickup);

/// <summary>
/// Cotação de frete. A implementação inicial é tabela própria; o Melhor Envio
/// entra depois como outro provedor, sem mudar o checkout.
/// </summary>
public interface IShippingProvider
{
    Task<IReadOnlyList<ShippingOption>> QuoteAsync(
        ShippingQuoteRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ShippingBand
{
    public string CepPrefix { get; set; } = string.Empty;

    public int MaxWeightGrams { get; set; }

    public decimal Price { get; set; }

    public int Days { get; set; }
}

public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    public string OriginZip { get; set; } = "18300000";

    /// <summary>Subtotal a partir do qual o frete sai de graça.</summary>
    public decimal FreeShippingThreshold { get; set; } = 150m;

    /// <summary>Dias úteis de preparo somados ao prazo da transportadora.</summary>
    public int HandlingDays { get; set; } = 2;

    public bool PickupEnabled { get; set; } = true;

    public string PickupLabel { get; set; } = "Retirar em Tatuí";

    public string PickupInstructions { get; set; } = "Combinar horário por WhatsApp.";

    /// <summary>Peso usado quando a variação não informa o seu.</summary>
    public int DefaultItemWeightGrams { get; set; } = 150;

    public List<ShippingBand> Bands { get; set; } = [];
}
