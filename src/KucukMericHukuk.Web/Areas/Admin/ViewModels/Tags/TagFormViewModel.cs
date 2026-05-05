using System.ComponentModel.DataAnnotations;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Tags;

public class TagFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public List<TagTranslationFormViewModel> Translations { get; set; } = new();
}

public class TagTranslationFormViewModel
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Etiket Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "URL Adresi (Slug)")]
    public string? Slug { get; set; }
}
