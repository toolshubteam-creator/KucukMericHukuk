using System.ComponentModel.DataAnnotations;
using KucukMericHukuk.Core.DTOs.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Attorneys;

public class AttorneyFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Bağlı Kullanıcı")]
    public int? UserId { get; set; }

    [Display(Name = "Profil Fotoğrafı (URL)")]
    public string? ProfileImageUrl { get; set; }

    [Display(Name = "Baro Sicil No")]
    public string? BarRegistrationNumber { get; set; }

    [Display(Name = "Baro Adı")]
    public string? BarName { get; set; }

    [Display(Name = "E-posta")]
    public string? Email { get; set; }

    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "LinkedIn URL")]
    public string? LinkedInUrl { get; set; }

    [Display(Name = "Bağlı Hizmetler")]
    public List<int> ServiceIds { get; set; } = new();

    public List<AttorneyTranslationFormViewModel> Translations { get; set; } = new();

    [BindNever]
    public List<LookupDto> AvailableUsers { get; set; } = new();

    [BindNever]
    public List<LookupDto> AvailableServices { get; set; } = new();
}

public class AttorneyTranslationFormViewModel
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Ünvan")]
    public string? Title { get; set; }

    [Display(Name = "URL Adresi (Slug)")]
    public string? Slug { get; set; }

    [Display(Name = "Kısa Biyografi")]
    public string? ShortBio { get; set; }

    [Display(Name = "Detaylı Biyografi")]
    public string? FullBio { get; set; }

    [Display(Name = "Eğitim")]
    public string? Education { get; set; }

    [Display(Name = "Yayınlar")]
    public string? Publications { get; set; }

    [Display(Name = "Meta Başlık")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    public string? MetaDescription { get; set; }
}
