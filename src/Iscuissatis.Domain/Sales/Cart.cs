using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Sales;

/// <summary>
/// Sacola do cliente. Vive por cookie (visitante) ou pela conta (cliente logado).
/// Todos os totais são calculados aqui, nunca aceitos do navegador.
/// </summary>
public sealed class Cart
{
    private readonly List<CartItem> _items = [];

    private Cart()
    {
    }

    private Cart(string? anonymousId, Guid? customerId, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        AnonymousId = anonymousId;
        CustomerId = customerId;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public string? AnonymousId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items;

    public int ItemCount => _items.Sum(item => item.Quantity);

    public decimal Subtotal => Math.Round(_items.Sum(item => item.LineTotal), 2, MidpointRounding.AwayFromZero);

    public static Cart Create(string? anonymousId, Guid? customerId, DateTimeOffset now)
    {
        var token = string.IsNullOrWhiteSpace(anonymousId) ? null : anonymousId.Trim();

        if (token is null && customerId is null)
        {
            throw new DomainException("O carrinho precisa de um visitante ou de um cliente.");
        }

        return new Cart(token, customerId, now);
    }

    public CartItem AddItem(
        Guid productVariantId,
        string productName,
        string variantName,
        decimal unitPrice,
        int quantity,
        int availableStock,
        DateTimeOffset now)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new DomainException("Escolha uma variação do produto.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("A quantidade precisa ser maior que zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("O preço não pode ser negativo.");
        }

        var existing = _items.FirstOrDefault(item => item.ProductVariantId == productVariantId);
        var newQuantity = (existing?.Quantity ?? 0) + quantity;

        EnsureStock(productVariantId, newQuantity, availableStock);

        if (existing is not null)
        {
            existing.ChangeQuantity(newQuantity);
        }
        else
        {
            _items.Add(new CartItem(
                Id,
                productVariantId,
                productName.Trim(),
                variantName.Trim(),
                unitPrice,
                quantity));
        }

        UpdatedAtUtc = now;

        return _items.Single(item => item.ProductVariantId == productVariantId);
    }

    public void ChangeQuantity(Guid productVariantId, int quantity, int availableStock, DateTimeOffset now)
    {
        var item = _items.FirstOrDefault(line => line.ProductVariantId == productVariantId)
            ?? throw new DomainException("Este item não está na sacola.");

        if (quantity < 0)
        {
            throw new DomainException("A quantidade não pode ser negativa.");
        }

        if (quantity == 0)
        {
            _items.Remove(item);
            UpdatedAtUtc = now;
            return;
        }

        EnsureStock(productVariantId, quantity, availableStock);
        item.ChangeQuantity(quantity);
        UpdatedAtUtc = now;
    }

    public void RemoveItem(Guid productVariantId, DateTimeOffset now)
    {
        var item = _items.FirstOrDefault(line => line.ProductVariantId == productVariantId);

        if (item is null)
        {
            return;
        }

        _items.Remove(item);
        UpdatedAtUtc = now;
    }

    /// <summary>
    /// Traz os itens do carrinho anônimo ao entrar na conta. Itens iguais somam,
    /// sempre limitados ao estoque disponível.
    /// </summary>
    public void MergeFrom(Cart guest, Func<Guid, int> availableStock, DateTimeOffset now)
    {
        foreach (var guestItem in guest.Items.ToList())
        {
            var stock = availableStock(guestItem.ProductVariantId);

            if (stock <= 0)
            {
                continue;
            }

            var existing = _items.FirstOrDefault(item => item.ProductVariantId == guestItem.ProductVariantId);
            var mergedQuantity = Math.Min((existing?.Quantity ?? 0) + guestItem.Quantity, stock);

            if (existing is not null)
            {
                existing.ChangeQuantity(mergedQuantity);
            }
            else
            {
                _items.Add(new CartItem(
                    Id,
                    guestItem.ProductVariantId,
                    guestItem.ProductName,
                    guestItem.VariantName,
                    guestItem.UnitPrice,
                    mergedQuantity));
            }
        }

        UpdatedAtUtc = now;
    }

    public void Clear(DateTimeOffset now)
    {
        _items.Clear();
        UpdatedAtUtc = now;
    }

    public void AttachToCustomer(Guid customerId, DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Cliente inválido.");
        }

        CustomerId = customerId;
        AnonymousId = null;
        UpdatedAtUtc = now;
    }

    private static void EnsureStock(Guid productVariantId, int requested, int availableStock)
    {
        if (requested > availableStock)
        {
            throw new DomainException(
                $"Estoque insuficiente para esta variação: disponível {availableStock}, solicitado {requested}.");
        }
    }
}
