using System.ComponentModel.DataAnnotations;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Articles;

public class ArticleFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Yazar")]
    public int? AuthorId { get; set; }

    public string? AuthorName { get; set; }

    [Display(Name = "Editör")]
    public int? EditorId { get; set; }

    public string? EditorName { get; set; }

    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    [Display(Name = "Öne Çıkan Görsel")]
    [StringLength(500)]
    public string? FeaturedImageUrl { get; set; }

    [Display(Name = "Yayın Tarihi")]
    [DataType(DataType.DateTime)]
    public DateTime? PublishedAt { get; set; }

    [Display(Name = "Durum")]
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    [Display(Name = "Öne Çıkar")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Etiketler")]
    public List<int> TagIds { get; set; } = new();

    public List<ArticleTranslationFormViewModel> Translations { get; set; } = new();

    public IReadOnlyList<LookupDto> Categories { get; set; } = Array.Empty<LookupDto>();
    public IReadOnlyList<LookupDto> AvailableTags { get; set; } = Array.Empty<LookupDto>();

    public IReadOnlyList<SelectListItem> AuthorOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> EditorOptions { get; set; } = Array.Empty<SelectListItem>();

    /// <summary>
    /// Author rolündeki kullanıcı kendi makalesini düzenliyorsa AuthorId+EditorId
    /// alanları UI'da disabled görünür (controller seviyesinde de hijack koruma var).
    /// </summary>
    public bool IsAuthorshipDisabled { get; set; }
}

public class ArticleTranslationFormViewModel
{
    public int? Id { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Başlık")]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "URL Adresi (Slug)")]
    [StringLength(300)]
    public string? Slug { get; set; }

    [Display(Name = "Kısa Açıklama")]
    [StringLength(500)]
    public string? Excerpt { get; set; }

    [Display(Name = "İçerik")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Meta Başlık")]
    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    [StringLength(500)]
    public string? MetaDescription { get; set; }
}
