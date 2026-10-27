using System.ComponentModel.DataAnnotations;

namespace Iscuissatis.Web.Models;

public class ProductFormModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do produto.")]
    [StringLength(160, ErrorMessage = "O nome pode ter no máximo 160 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha uma categoria.")]
    [Display(Name = "Categoria")]
    public Guid CategoryId { get; set; }

    [StringLength(4000)]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }

    [StringLength(2000)]
    [Display(Name = "Ingredientes (INCI)")]
    public string? Ingredients { get; set; }

    [StringLength(2000)]
    [Display(Name = "Modo de uso")]
    public string? HowToUse { get; set; }

    [StringLength(2000)]
    [Display(Name = "Precauções")]
    public string? Precautions { get; set; }

    [Range(0, 100000, ErrorMessage = "O peso líquido deve ficar entre 0 e 100000 g.")]
    [Display(Name = "Peso líquido (g)")]
    public int? NetWeightGrams { get; set; }
}

public class VariantFormModel
{
    [Required(ErrorMessage = "Informe o nome da variação.")]
    [StringLength(120)]
    [Display(Name = "Nome da variação")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o SKU.")]
    [StringLength(60)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "O preço não pode ser negativo.")]
    [Display(Name = "Preço (R$)")]
    public decimal Price { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Preço promocional (R$)")]
    public decimal? PromotionalPrice { get; set; }

    [Range(0, 1000000, ErrorMessage = "O estoque não pode ser negativo.")]
    [Display(Name = "Estoque")]
    public int StockQuantity { get; set; }

    [Range(0, 100000, ErrorMessage = "O peso deve ficar entre 0 e 100000 g.")]
    [Display(Name = "Peso da embalagem (g)")]
    public int WeightGrams { get; set; }
}
