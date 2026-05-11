using System.ComponentModel.DataAnnotations;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Faqs;

public class FaqFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public List<FaqTranslationFormViewModel> Translations { get; set; } = new();
}

public class FaqTranslationFormViewModel
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Soru")]
    public string Question { get; set; } = string.Empty;

    [Display(Name = "Cevap")]
    public string Answer { get; set; } = string.Empty;
}
