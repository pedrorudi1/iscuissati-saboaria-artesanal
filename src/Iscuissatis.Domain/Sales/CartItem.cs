using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Sales;

public sealed class CartItem
{
    private CartItem()
    {
    }

    internal CartItem(
        Guid cartId,
        Guid productVariantId,
        string productName,
        string variantName,
        decimal unitPrice,
        int quantity)
    {
        Id = Guid.NewGuid();
        CartId = cartId;
        ProductVariantId = productVariantId;
        ProductName = productName;
        VariantName = variantName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }

    public Guid CartId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    /// <summary>Nome congelado no momento da adição: o catálogo pode mudar depois.</summary>
    public string ProductName { get; private set; } = string.Empty;

    public string VariantName { get; private set; } = string.Empty;

    /// <summary>Preço unitário congelado. O checkout reconfere o preço atual.</summary>
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("A quantidade precisa ser maior que zero.");
        }

        Quantity = quantity;
    }
}
