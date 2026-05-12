using System.ComponentModel.DataAnnotations;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Medias;

public class MediaEditFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Alternatif Metin (Alt-text)")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? AltText { get; set; }

    [Display(Name = "Galeride Göster (Public)")]
    public bool IsPublic { get; set; }
}
