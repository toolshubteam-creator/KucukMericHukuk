using System.ComponentModel.DataAnnotations;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Testimonials;

public class TestimonialFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "İnisyal/Rumuz")]
    public string? AuthorInitials { get; set; }

    [Display(Name = "Rol")]
    public string? AuthorRole { get; set; } = "Müvekkil";

    [Display(Name = "Yıldız (1-5)")]
    public int? Rating { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Ana Sayfada Öne Çıkar")]
    public bool IsFeatured { get; set; }

    public List<TestimonialTranslationFormViewModel> Translations { get; set; } = new();
}

public class TestimonialTranslationFormViewModel
{
    public int? Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Yorum")]
    public string Content { get; set; } = string.Empty;
}
