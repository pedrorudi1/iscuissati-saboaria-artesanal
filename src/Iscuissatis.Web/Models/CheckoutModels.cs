using System.ComponentModel.DataAnnotations;

namespace Iscuissatis.Web.Models;

public class CheckoutFormModel
{
    [Required(ErrorMessage = "Informe o nome de quem recebe.")]
    [StringLength(160)]
    [Display(Name = "Nome de quem recebe")]
    public string Recipient { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CEP.")]
    [StringLength(9, MinimumLength = 8, ErrorMessage = "CEP deve ter 8 dígitos.")]
    [Display(Name = "CEP")]
    public string ZipCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a rua.")]
    [StringLength(200)]
    [Display(Name = "Rua")]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o número.")]
    [StringLength(20)]
    [Display(Name = "Número")]
    public string Number { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Complemento (opcional)")]
    public string? Complement { get; set; }

    [StringLength(120)]
    [Display(Name = "Bairro")]
    public string? District { get; set; }

    [Required(ErrorMessage = "Informe a cidade.")]
    [StringLength(120)]
    [Display(Name = "Cidade")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o estado.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Use a sigla do estado (ex.: SP).")]
    [Display(Name = "Estado (UF)")]
    public string State { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(180)]
    [Display(Name = "E-mail para contato")]
    public string? GuestEmail { get; set; }

    [Required(ErrorMessage = "Escolha uma opção de entrega.")]
    public string ShippingCode { get; set; } = string.Empty;
}
