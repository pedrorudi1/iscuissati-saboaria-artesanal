using Iscuissatis.Domain.Common;

namespace Iscuissatis.Domain.Catalog;

public sealed class ProductVariant
{
    private ProductVariant()
    {
    }

    private ProductVariant(
        Guid productId,
        string name,
        string sku,
        decimal price,
        int stockQuantity,
        int weightGrams,
        decimal lengthCm,
        decimal widthCm,
        decimal heightCm,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        WeightGrams = weightGrams;
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
        IsActive = true;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    /// <summary>Fragrância, tamanho ou apresentação. Ex.: "Lavanda 100 g".</summary>
    public string Name { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public decimal? PromotionalPrice { get; private set; }

    public int StockQuantity { get; private set; }

    /// <summary>Peso da embalagem, usado no cálculo de frete.</summary>
    public int WeightGrams { get; private set; }

    public decimal LengthCm { get; private set; }

    public decimal WidthCm { get; private set; }

    public decimal HeightCm { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Preço a ser cobrado: promocional quando houver, senão o cheio.</summary>
    public decimal EffectivePrice => PromotionalPrice ?? Price;

    public static ProductVariant Create(
        Guid productId,
        string name,
        string sku,
        decimal price,
        int stockQuantity,
        int weightGrams,
        decimal lengthCm,
        decimal widthCm,
        decimal heightCm,
        decimal? promotionalPrice,
        DateTimeOffset now)
    {
        var normalizedSku = NormalizeSku(sku);

        if ((name ?? string.Empty).Trim().Length == 0)
        {
            throw new DomainException("O nome da variação é obrigatório.");
        }

        ValidatePrice(price);
        ValidateStock(stockQuantity);

        if (weightGrams < 0)
        {
            throw new DomainException("O peso da variação não pode ser negativo.");
        }

        if (promotionalPrice is not null)
        {
            ValidatePromotionalPrice(promotionalPrice.Value, price);
        }

        return new ProductVariant(
            productId,
            name!.Trim(),
            normalizedSku,
            price,
            stockQuantity,
            weightGrams,
            lengthCm,
            widthCm,
            heightCm,
            now)
        {
            PromotionalPrice = promotionalPrice,
        };
    }

    public void SetPrice(decimal price, DateTimeOffset now)
    {
        ValidatePrice(price);

        if (PromotionalPrice is not null && PromotionalPrice >= price)
        {
            throw new DomainException("O preço promocional precisa ser menor que o novo preço.");
        }

        Price = price;
        UpdatedAtUtc = now;
    }

    public void SetPromotionalPrice(decimal? promotionalPrice, DateTimeOffset now)
    {
        if (promotionalPrice is not null)
        {
            ValidatePromotionalPrice(promotionalPrice.Value, Price);
        }

        PromotionalPrice = promotionalPrice;
        UpdatedAtUtc = now;
    }

    /// <summary>
    /// Ajuste manual de estoque. Valores negativos representam saída.
    /// Nunca deixa o estoque negativo: recusa a operação inteira.
    /// </summary>
    public void AdjustStock(int delta, DateTimeOffset now)
    {
        var newQuantity = StockQuantity + delta;

        if (newQuantity < 0)
        {
            throw new DomainException(
                $"Estoque insuficiente: disponível {StockQuantity}, solicitado {Math.Abs(delta)}.");
        }

        StockQuantity = newQuantity;
        UpdatedAtUtc = now;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string NormalizeSku(string sku)
    {
        var normalized = (sku ?? string.Empty).Trim().ToUpperInvariant();

        if (normalized.Length == 0)
        {
            throw new DomainException("O SKU é obrigatório.");
        }

        return normalized;
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("O preço não pode ser negativo.");
        }
    }

    private static void ValidateStock(int stockQuantity)
    {
        if (stockQuantity < 0)
        {
            throw new DomainException("O estoque não pode ser negativo.");
        }
    }

    private static void ValidatePromotionalPrice(decimal promotionalPrice, decimal price)
    {
        if (promotionalPrice <= 0)
        {
            throw new DomainException("O preço promocional precisa ser maior que zero.");
        }

        if (promotionalPrice >= price)
        {
            throw new DomainException("O preço promocional precisa ser menor que o preço cheio.");
        }
    }
}
