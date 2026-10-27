using System.ComponentModel.DataAnnotations;

namespace Iscuissatis.Web.Models;

public class CategoryFormModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da categoria.")]
    [StringLength(120, ErrorMessage = "O nome pode ter no máximo 120 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [StringLength(600, ErrorMessage = "A descrição pode ter no máximo 600 caracteres.")]
    [Display(Name = "Descrição (opcional)")]
    public string? Description { get; set; }

    [Range(0, 999, ErrorMessage = "A ordem deve ficar entre 0 e 999.")]
    [Display(Name = "Ordem de exibição")]
    public int SortOrder { get; set; }
}
