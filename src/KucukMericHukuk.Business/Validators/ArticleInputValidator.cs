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
            .NotNull().WithMessage("Çeviri listesi gereklidir.");

        RuleFor(x => x.TagIds)
            .NotNull().WithMessage("Etiket listesi gereklidir.");

        RuleFor(x => x.Translations)
            .Must(list => list != null && list.Any(t =>
                !string.IsNullOrWhiteSpace(t.Title) && !string.IsNullOrWhiteSpace(t.Content)))
            .WithMessage("En az bir dilde başlık ve içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new ArticleTranslationInputValidator());
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
            .Must(code => LanguageCodes.Supported.Contains(code))
            .WithMessage("Geçersiz dil kodu.");

        RuleFor(x => x.Title)
            .MaximumLength(300).WithMessage("Başlık en fazla 300 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(300).WithMessage("Slug en fazla 300 karakter olabilir.")
                .Matches(SlugRegex).WithMessage(
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        });

        RuleFor(x => x.Excerpt)
            .MaximumLength(500).WithMessage("Kısa açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta açıklama en fazla 500 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Title), () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Başlık girilmiş bir dilde içerik de zorunludur.");
        });
    }
}
