using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Sales;

public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    Preparing = 2,
    Shipped = 3,
    Delivered = 4,
    Canceled = 5,
    Refunded = 6,
}

/// <summary>
/// Endereço congelado no pedido. É classe (e não record) de propósito: o EF mapeia
/// como owned type com chave de identificação e não consegue atualizar o pedido
/// quando o valor é imutável por referência.
/// </summary>
public sealed class ShippingAddress
{
    private ShippingAddress()
    {
    }

    private ShippingAddress(
        string recipient,
        string street,
        string number,
        string? complement,
        string district,
        string city,
        string state,
        string zipCode)
    {
        Recipient = recipient;
        Street = street;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public string Recipient { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string? Complement { get; private set; }

    public string District { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    public static ShippingAddress Create(
        string recipient,
        string street,
        string number,
        string? complement,
        string district,
        string city,
        string state,
        string zipCode)
    {
        if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(street)
            || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
        {
            throw new DomainException("Endereço incompleto: informe destinatário, rua, cidade e estado.");
        }

        var digits = new string((zipCode ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length != 8)
        {
            throw new DomainException("CEP inválido: informe os 8 dígitos.");
        }

        return new ShippingAddress(
            recipient.Trim(),
            street.Trim(),
            (number ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(complement) ? null : complement.Trim(),
            (district ?? string.Empty).Trim(),
            city.Trim(),
            state.Trim().ToUpperInvariant(),
            digits);
    }
}

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    internal OrderItem(
        Guid orderId,
        Guid productVariantId,
        string productName,
        string variantName,
        decimal unitPrice,
        int quantity)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductVariantId = productVariantId;
        ProductName = productName;
        VariantName = variantName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    /// <summary>Nome congelado: o catálogo pode mudar depois da compra.</summary>
    public string ProductName { get; private set; } = string.Empty;

    public string VariantName { get; private set; } = string.Empty;

    /// <summary>Preço congelado no momento da compra.</summary>
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
}

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    private Order(
        string number,
        Guid? customerId,
        string? guestEmail,
        ShippingAddress address,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        Number = number;
        CustomerId = customerId;
        GuestEmail = guestEmail;
        ShippingAddress = address;
        Status = OrderStatus.PendingPayment;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public Guid? CustomerId { get; private set; }

    public string? GuestEmail { get; private set; }

    public OrderStatus Status { get; private set; }

    public ShippingAddress ShippingAddress { get; private set; } = null!;

    public decimal Subtotal { get; private set; }

    public decimal Shipping { get; private set; }

    public decimal Discount { get; private set; }

    public decimal Total => Math.Max(0m, Math.Round(Subtotal + Shipping - Discount, 2, MidpointRounding.AwayFromZero));

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? PaidAtUtc { get; private set; }

    public DateTimeOffset? CanceledAtUtc { get; private set; }

    public string? TrackingCode { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public static Order Create(
        string number,
        Guid? customerId,
        string? guestEmail,
        ShippingAddress address,
        IReadOnlyList<(Guid VariantId, string ProductName, string VariantName, decimal UnitPrice, int Quantity)> items,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new DomainException("O pedido precisa de um número.");
        }

        if (customerId is null && string.IsNullOrWhiteSpace(guestEmail))
        {
            throw new DomainException("Informe o e-mail para contato do pedido.");
        }

        if (items.Count == 0)
        {
            throw new DomainException("Não é possível criar pedido sem itens.");
        }

        // Clona o endereço: o EF mapeia como owned type e a mesma instância não pode
        // pertencer a dois pedidos.
        var addressCopy = ShippingAddress.Create(
            address.Recipient, address.Street, address.Number, address.Complement,
            address.District, address.City, address.State, address.ZipCode);

        var order = new Order(number.Trim(), customerId, guestEmail?.Trim().ToLowerInvariant(), addressCopy, now);

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                throw new DomainException("Quantidade inválida no pedido.");
            }

            if (item.UnitPrice < 0)
            {
                throw new DomainException("Preço inválido no pedido.");
            }

            order._items.Add(new OrderItem(
                order.Id, item.VariantId, item.ProductName, item.VariantName, item.UnitPrice, item.Quantity));
        }

        order.Subtotal = Math.Round(order._items.Sum(item => item.LineTotal), 2, MidpointRounding.AwayFromZero);

        return order;
    }

    public void SetShipping(decimal shipping, DateTimeOffset now)
    {
        if (shipping < 0)
        {
            throw new DomainException("O frete não pode ser negativo.");
        }

        Shipping = shipping;
        UpdatedAtUtc = now;
    }

    public void ApplyDiscount(decimal discount, DateTimeOffset now)
    {
        if (discount < 0)
        {
            throw new DomainException("O desconto não pode ser negativo.");
        }

        if (discount > Subtotal + Shipping)
        {
            throw new DomainException("O desconto não pode ser maior que o total.");
        }

        Discount = discount;
        UpdatedAtUtc = now;
    }

    public void MarkAsPaid(DateTimeOffset now)
    {
        EnsureTransition(OrderStatus.Paid, OrderStatus.PendingPayment);
        Status = OrderStatus.Paid;
        PaidAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void StartPreparing(DateTimeOffset now)
    {
        EnsureTransition(OrderStatus.Preparing, OrderStatus.Paid);
        Status = OrderStatus.Preparing;
        UpdatedAtUtc = now;
    }

    public void Ship(string trackingCode, DateTimeOffset now)
    {
        EnsureTransition(OrderStatus.Shipped, OrderStatus.Preparing);

        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            throw new DomainException("Informe o código de rastreio para despachar.");
        }

        TrackingCode = trackingCode.Trim();
        Status = OrderStatus.Shipped;
        UpdatedAtUtc = now;
    }

    public void MarkAsDelivered(DateTimeOffset now)
    {
        EnsureTransition(OrderStatus.Delivered, OrderStatus.Shipped);
        Status = OrderStatus.Delivered;
        UpdatedAtUtc = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Canceled or OrderStatus.Refunded)
        {
            throw new DomainException($"Não é possível cancelar um pedido {Status}.");
        }

        Status = OrderStatus.Canceled;
        CanceledAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Refund(DateTimeOffset now)
    {
        if (Status is not (OrderStatus.Paid or OrderStatus.Preparing or OrderStatus.Shipped or OrderStatus.Delivered))
        {
            throw new DomainException($"Não é possível reembolsar um pedido {Status}.");
        }

        Status = OrderStatus.Refunded;
        UpdatedAtUtc = now;
    }

    private void EnsureTransition(OrderStatus target, OrderStatus required)
    {
        if (Status != required)
        {
            throw new DomainException($"Transição inválida: {Status} → {target}. Era esperado {required}.");
        }
    }
}

public sealed class StockReservation
{
    private StockReservation()
    {
    }

    public StockReservation(Guid orderId, Guid productVariantId, int quantity, DateTimeOffset expiresAtUtc)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    public bool IsReleased => ReleasedAtUtc is not null;

    public void Release(DateTimeOffset now)
    {
        if (IsReleased)
        {
            return;
        }

        ReleasedAtUtc = now;
    }
}
