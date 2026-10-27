using Iscuissatis.Application.Shipping;
using Microsoft.Extensions.Options;

namespace Iscuissatis.Infrastructure.Shipping;

/// <summary>
/// Tabela própria por região (1º dígito do CEP) e peso.
/// Sempre disponível: é o fallback quando um provedor externo cair.
/// </summary>
public sealed class TableShippingProvider(IOptions<ShippingOptions> options) : IShippingProvider
{
    private readonly ShippingOptions _options = options.Value;

    public Task<IReadOnlyList<ShippingOption>> QuoteAsync(
        ShippingQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var digits = new string((request.ZipCode ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length != 8)
        {
            return Task.FromResult<IReadOnlyList<ShippingOption>>([]);
        }

        var prefix = digits[..1];
        var weight = request.TotalWeightGrams <= 0
            ? _options.DefaultItemWeightGrams
            : request.TotalWeightGrams;

        var result = new List<ShippingOption>();

        var band = _options.Bands
            .Where(item => item.CepPrefix == prefix && weight <= item.MaxWeightGrams)
            .OrderBy(item => item.Price)
            .FirstOrDefault();

        if (band is not null)
        {
            var isFree = _options.FreeShippingThreshold > 0
                         && request.Subtotal >= _options.FreeShippingThreshold;

            result.Add(new ShippingOption(
                "tabela",
                isFree ? "Entrega (frete grátis)" : "Entrega pela transportadora",
                isFree ? 0m : band.Price,
                band.Days + _options.HandlingDays,
                IsPickup: false));
        }

        if (_options.PickupEnabled)
        {
            result.Add(new ShippingOption(
                "retirada",
                _options.PickupLabel,
                0m,
                _options.HandlingDays,
                IsPickup: true));
        }

        return Task.FromResult<IReadOnlyList<ShippingOption>>(result);
    }
}
