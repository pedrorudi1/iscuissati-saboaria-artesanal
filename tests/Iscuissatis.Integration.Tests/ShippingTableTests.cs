using Iscuissatis.Application.Shipping;
using Iscuissatis.Infrastructure.Shipping;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

public class ShippingTableTests
{
    private static TableShippingProvider Provider(
        decimal freeThreshold = 150m,
        bool pickup = true,
        params ShippingBand[] bands)
        => new(Options.Create(new ShippingOptions
        {
            FreeShippingThreshold = freeThreshold,
            HandlingDays = 2,
            PickupEnabled = pickup,
            PickupLabel = "Retirar em Tatuí",
            DefaultItemWeightGrams = 150,
            Bands = bands.Length > 0
                ? bands.ToList()
                : [new ShippingBand { CepPrefix = "1", MaxWeightGrams = 500, Price = 18.90m, Days = 3 }],
        }));

    [Fact]
    public async Task Free_shipping_applies_when_subtotal_reaches_the_threshold()
    {
        var options = await Provider().QuoteAsync(new ShippingQuoteRequest("18300000", 300, 150m));

        var delivery = options.Single(option => !option.IsPickup);
        delivery.Price.ShouldBe(0m);
        delivery.Name.ShouldContain("grátis");
    }

    [Fact]
    public async Task Below_the_threshold_charges_the_band_price_plus_handling_days()
    {
        var options = await Provider().QuoteAsync(new ShippingQuoteRequest("18300000", 300, 40m));

        var delivery = options.Single(option => !option.IsPickup);
        delivery.Price.ShouldBe(18.90m);
        delivery.EstimatedBusinessDays.ShouldBe(5); // 3 da faixa + 2 de preparo
    }

    [Fact]
    public async Task Heavier_orders_use_the_next_weight_band()
    {
        var provider = Provider(bands:
        [
            new ShippingBand { CepPrefix = "1", MaxWeightGrams = 500, Price = 18.90m, Days = 3 },
            new ShippingBand { CepPrefix = "1", MaxWeightGrams = 2000, Price = 24.90m, Days = 4 },
        ]);

        var options = await provider.QuoteAsync(new ShippingQuoteRequest("18300000", 900, 50m));

        options.Single(option => !option.IsPickup).Price.ShouldBe(24.90m);
    }

    [Fact]
    public async Task Zip_code_without_band_returns_no_delivery_option()
    {
        var options = await Provider().QuoteAsync(new ShippingQuoteRequest("90000000", 300, 50m));

        options.ShouldNotContain(option => !option.IsPickup);
    }

    [Fact]
    public async Task Pickup_is_free_and_can_be_disabled()
    {
        var withPickup = await Provider().QuoteAsync(new ShippingQuoteRequest("18300000", 300, 50m));
        withPickup.Single(option => option.IsPickup).Price.ShouldBe(0m);

        var withoutPickup = await Provider(pickup: false)
            .QuoteAsync(new ShippingQuoteRequest("18300000", 300, 50m));
        withoutPickup.ShouldNotContain(option => option.IsPickup);
    }
}
