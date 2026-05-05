using System.ComponentModel.DataAnnotations;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Pages;

public class PageFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Sayfa Anahtarı")]
    public string PageKey { get; set; } = string.Empty;

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Sistem Sayfası")]
    public bool IsSystem { get; set; }

    public List<PageTranslationFormViewModel> Translations { get; set; } = new();
}

public class PageTranslationFormViewModel
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "URL Adresi (Slug)")]
    public string? Slug { get; set; }

    [Display(Name = "İçerik")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Meta Başlık")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    public string? MetaDescription { get; set; }
}
