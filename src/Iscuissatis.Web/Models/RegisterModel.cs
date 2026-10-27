using System.ComponentModel.DataAnnotations;

namespace Iscuissatis.Web.Models;

public class RegisterModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120)]
    [Display(Name = "Nome")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha uma senha.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "A senha precisa ter ao menos 10 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Repita a senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
