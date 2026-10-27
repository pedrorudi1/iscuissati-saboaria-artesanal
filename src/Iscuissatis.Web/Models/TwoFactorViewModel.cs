using System.ComponentModel.DataAnnotations;

namespace Iscuissatis.Web.Models;

public class TwoFactorViewModel
{
    [Required(ErrorMessage = "Informe o código de 6 dígitos.")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "Código inválido.")]
    [Display(Name = "Código do autenticador")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
