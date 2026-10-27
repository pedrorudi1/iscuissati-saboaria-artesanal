using Iscuissatis.Application.Catalog;
using Iscuissatis.Domain.Common;
using Iscuissatis.Domain.Sales;

namespace Iscuissatis.Application.Sales;

/// <summary>
/// Casos de uso da sacola. Toda quantidade e todo total são conferidos aqui,
/// contra o estoque atual — nada vem do navegador.
/// </summary>
public sealed class CartService(
    ICartRepository carts,
    IProductRepository products,
    TimeProvider clock)
{
    public async Task<Cart> GetOrCreateAsync(
        string anonymousId,
        CancellationToken cancellationToken = default)
    {
        var cart = await carts.FindByAnonymousIdAsync(anonymousId, cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = Cart.Create(anonymousId, null, clock.GetUtcNow());
        carts.Add(cart);
        await carts.SaveChangesAsync(cancellationToken);

        return cart;
    }

    public Task<Cart?> GetAsync(string anonymousId, CancellationToken cancellationToken = default)
        => carts.FindByAnonymousIdAsync(anonymousId, cancellationToken);

    public async Task<Cart> AddItemAsync(
        string anonymousId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateAsync(anonymousId, cancellationToken);
        var variant = await GetPurchasableVariantAsync(productVariantId, cancellationToken);

        cart.AddItem(
            variant.VariantId,
            variant.ProductName,
            variant.VariantName,
            variant.Price,
            quantity,
            variant.StockQuantity,
            clock.GetUtcNow());

        await carts.SaveChangesAsync(cancellationToken);

        return cart;
    }

    public async Task<Cart> ChangeQuantityAsync(
        string anonymousId,
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateAsync(anonymousId, cancellationToken);
        var variant = await GetPurchasableVariantAsync(productVariantId, cancellationToken);

        cart.ChangeQuantity(
            productVariantId,
            quantity,
            variant.StockQuantity,
            clock.GetUtcNow());

        await carts.SaveChangesAsync(cancellationToken);

        return cart;
    }

    public async Task<Cart> RemoveItemAsync(
        string anonymousId,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateAsync(anonymousId, cancellationToken);

        cart.RemoveItem(productVariantId, clock.GetUtcNow());
        await carts.SaveChangesAsync(cancellationToken);

        return cart;
    }

    public async Task ClearCartAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        cart.Clear(clock.GetUtcNow());
        await carts.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(string anonymousId, CancellationToken cancellationToken = default)
    {
        var cart = await carts.FindByAnonymousIdAsync(anonymousId, cancellationToken);

        if (cart is null)
        {
            return;
        }

        cart.Clear(clock.GetUtcNow());
        await carts.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Ao entrar na conta, a sacola anônima é absorvida pela sacola do cliente.
    /// </summary>
    public async Task<Cart> MergeAnonymousCartAsync(
        string anonymousId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customerCart = await carts.FindByCustomerIdAsync(customerId, cancellationToken);

        if (customerCart is null)
        {
            customerCart = Cart.Create(null, customerId, clock.GetUtcNow());
            carts.Add(customerCart);
        }

        var guestCart = await carts.FindByAnonymousIdAsync(anonymousId, cancellationToken);

        if (guestCart is not null && guestCart.Id != customerCart.Id)
        {
            var stocks = new Dictionary<Guid, int>();

            foreach (var item in guestCart.Items)
            {
                var variant = await products.GetVariantSnapshotAsync(item.ProductVariantId, cancellationToken);
                stocks[item.ProductVariantId] = variant?.StockQuantity ?? 0;
            }

            customerCart.MergeFrom(guestCart, id => stocks.GetValueOrDefault(id), clock.GetUtcNow());
        }

        await carts.SaveChangesAsync(cancellationToken);

        return customerCart;
    }

    private async Task<VariantSnapshot> GetPurchasableVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        var variant = await products.GetVariantSnapshotAsync(productVariantId, cancellationToken);

        if (variant is null || !variant.IsActive || !variant.IsPublished)
        {
            throw new DomainException("Este produto não está disponível para compra.");
        }

        if (variant.StockQuantity <= 0)
        {
            throw new DomainException("Este produto está esgotado.");
        }

        return variant;
    }
}
