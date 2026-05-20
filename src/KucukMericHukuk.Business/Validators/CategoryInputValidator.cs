using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Category;

namespace KucukMericHukuk.Business.Validators;

public class CategoryInputValidator : AbstractValidator<CategoryInputDto>
{
    public CategoryInputValidator()
    {
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        RuleFor(x => x).Must(x =>
            !x.Id.HasValue || x.ParentCategoryId != x.Id.Value)
            .WithMessage("Kategori kendi üst kategorisi olamaz.")
            .OverridePropertyName(nameof(CategoryInputDto.ParentCategoryId));

        RuleFor(x => x.Translations)
            .Must(list => list != null && list.Any(t => LanguageCodes.IsDefault(t.LanguageCode) && IsActive(t)))
            .WithMessage("Türkçe içerik zorunludur.");

        RuleForEach(x => x.Translations)
            .Where(IsActive)
            .SetValidator(new CategoryTranslationInputValidator());
    }

    private static bool IsActive(CategoryTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Name)
            || !string.IsNullOrWhiteSpace(t.Slug)
            || !string.IsNullOrWhiteSpace(t.Description)
            || !string.IsNullOrWhiteSpace(t.MetaTitle)
            || !string.IsNullOrWhiteSpace(t.MetaDescription);
    }
}

public class CategoryTranslationInputValidator : AbstractValidator<CategoryTranslationInputDto>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public CategoryTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .Must(LanguageCodes.IsSupported).WithMessage("Geçersiz dil kodu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(150).WithMessage("Slug en fazla 150 karakter olabilir.")
                .Matches(SlugRegex).WithMessage("Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        });

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta açıklama en fazla 500 karakter olabilir.");
    }
}
