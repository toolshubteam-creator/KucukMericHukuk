using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Tag;

namespace KucukMericHukuk.Business.Validators;

public class TagInputValidator : AbstractValidator<TagInputDto>
{
    public TagInputValidator()
    {
        RuleFor(x => x.Translations)
            .Must(list => list != null && list.Any(t => LanguageCodes.IsDefault(t.LanguageCode) && IsActive(t)))
            .WithMessage("Türkçe içerik zorunludur.");

        RuleForEach(x => x.Translations)
            .Where(IsActive)
            .SetValidator(new TagTranslationInputValidator());
    }

    private static bool IsActive(TagTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Name)
            || !string.IsNullOrWhiteSpace(t.Slug);
    }
}

public class TagTranslationInputValidator : AbstractValidator<TagTranslationInputDto>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public TagTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .Must(LanguageCodes.IsSupported).WithMessage("Geçersiz dil kodu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Etiket adı zorunludur.")
            .MaximumLength(50).WithMessage("Etiket adı en fazla 50 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(50).WithMessage("Slug en fazla 50 karakter olabilir.")
                .Matches(SlugRegex).WithMessage(
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir (örn. 'icra-hukuku').");
        });
    }
}
