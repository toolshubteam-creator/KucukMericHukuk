using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Article;

namespace KucukMericHukuk.Business.Validators;

public class ArticleInputValidator : AbstractValidator<ArticleInputDto>
{
    public ArticleInputValidator()
    {
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.FeaturedImageUrl)
            .MaximumLength(500).WithMessage("Görsel URL en fazla 500 karakter olabilir.");

        RuleFor(x => x.Translations)
            .NotNull().WithMessage("Çeviri listesi gereklidir.")
            .Must(list => list != null && list.Any(t => LanguageCodes.IsDefault(t.LanguageCode) && IsActive(t)))
            .WithMessage("Türkçe başlık ve içerik zorunludur.");

        RuleFor(x => x.TagIds)
            .NotNull().WithMessage("Etiket listesi gereklidir.");

        RuleForEach(x => x.Translations)
            .Where(IsActive)
            .SetValidator(new ArticleTranslationInputValidator());
    }

    private static bool IsActive(ArticleTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Title)
            || !string.IsNullOrWhiteSpace(t.Slug)
            || !string.IsNullOrWhiteSpace(t.Excerpt)
            || !string.IsNullOrWhiteSpace(t.Content)
            || !string.IsNullOrWhiteSpace(t.MetaTitle)
            || !string.IsNullOrWhiteSpace(t.MetaDescription);
    }
}

public class ArticleTranslationInputValidator : AbstractValidator<ArticleTranslationInputDto>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public ArticleTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .Must(LanguageCodes.IsSupported)
            .WithMessage("Geçersiz dil kodu.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(300).WithMessage("Başlık en fazla 300 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(300).WithMessage("Slug en fazla 300 karakter olabilir.")
                .Matches(SlugRegex).WithMessage("Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        });

        RuleFor(x => x.Excerpt)
            .MaximumLength(500).WithMessage("Kısa açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik zorunludur.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta açıklama en fazla 500 karakter olabilir.");
    }
}
