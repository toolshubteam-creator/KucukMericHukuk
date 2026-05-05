using System.ComponentModel.DataAnnotations;
using KucukMericHukuk.Core.DTOs.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Services;

public class ServiceFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "İkon")]
    public string? Icon { get; set; }

    [Display(Name = "Öne Çıkan Görsel")]
    public string? FeaturedImage { get; set; }

    [Display(Name = "Bağlı Avukatlar")]
    public List<int> AttorneyIds { get; set; } = new();

    // POST'a gitmez — controller her render'da dolduruyor.
    [BindNever]
    public List<LookupDto> AvailableAttorneys { get; set; } = new();

    public List<ServiceTranslationFormViewModel> Translations { get; set; } = new();
}

public class ServiceTranslationFormViewModel
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Hizmet Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "URL Adresi (Slug)")]
    public string? Slug { get; set; }

    [Display(Name = "Kısa Açıklama")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Detaylı Açıklama")]
    public string FullDescription { get; set; } = string.Empty;

    [Display(Name = "Meta Başlık")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    public string? MetaDescription { get; set; }
}
