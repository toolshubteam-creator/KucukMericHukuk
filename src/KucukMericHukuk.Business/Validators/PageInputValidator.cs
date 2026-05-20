using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Page;

namespace KucukMericHukuk.Business.Validators;

public class PageInputValidator : AbstractValidator<PageInputDto>
{
    private static readonly Regex PageKeyRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public PageInputValidator()
    {
        RuleFor(x => x.PageKey)
            .NotEmpty().WithMessage("Sayfa anahtarı (PageKey) zorunludur.")
            .MaximumLength(100).WithMessage("Sayfa anahtarı en fazla 100 karakter olabilir.")
            .Matches(PageKeyRegex).WithMessage(
                "Sayfa anahtarı yalnızca küçük harf, rakam ve tire içerebilir (örn. 'hakkimizda').");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        RuleFor(x => x.Translations)
            .Must(list => list != null && list.Any(t => LanguageCodes.IsDefault(t.LanguageCode) && IsActive(t)))
            .WithMessage("Türkçe içerik zorunludur.");

        RuleForEach(x => x.Translations)
            .Where(IsActive)
            .SetValidator(new PageTranslationInputValidator());
    }

    private static bool IsActive(PageTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Title)
            || !string.IsNullOrWhiteSpace(t.Slug)
            || !string.IsNullOrWhiteSpace(t.Content)
            || !string.IsNullOrWhiteSpace(t.MetaTitle)
            || !string.IsNullOrWhiteSpace(t.MetaDescription);
    }
}

public class PageTranslationInputValidator : AbstractValidator<PageTranslationInputDto>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public PageTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .Must(LanguageCodes.IsSupported).WithMessage("Geçersiz dil kodu.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik zorunludur.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(200).WithMessage("Slug en fazla 200 karakter olabilir.")
                .Matches(SlugRegex).WithMessage(
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir (örn. 'icra-hukuku').");
        });

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta açıklama en fazla 500 karakter olabilir.");
    }
}
