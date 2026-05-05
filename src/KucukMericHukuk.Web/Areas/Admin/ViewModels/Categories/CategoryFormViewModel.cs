using System.ComponentModel.DataAnnotations;
using KucukMericHukuk.Core.DTOs.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Categories;

public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Üst Kategori")]
    public int? ParentCategoryId { get; set; }

    [Display(Name = "Sıra")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public List<CategoryTranslationFormViewModel> Translations { get; set; } = new();

    [BindNever]
    public List<LookupDto> AvailableParents { get; set; } = new();
}

public class CategoryTranslationFormViewModel
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "URL Adresi (Slug)")]
    public string? Slug { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Meta Başlık")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    public string? MetaDescription { get; set; }
}
