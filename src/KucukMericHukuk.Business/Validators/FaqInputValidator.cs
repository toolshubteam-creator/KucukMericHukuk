using FluentValidation;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Faq;

namespace KucukMericHukuk.Business.Validators;

public class FaqInputValidator : AbstractValidator<FaqInputDto>
{
    public FaqInputValidator()
    {
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        RuleFor(x => x.Translations)
            .Must(list => list != null && list.Any(t => LanguageCodes.IsDefault(t.LanguageCode) && IsActive(t)))
            .WithMessage("Türkçe içerik zorunludur.");

        RuleForEach(x => x.Translations)
            .Where(IsActive)
            .SetValidator(new FaqTranslationInputValidator());
    }

    private static bool IsActive(FaqTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Question)
            || !string.IsNullOrWhiteSpace(t.Answer);
    }
}

public class FaqTranslationInputValidator : AbstractValidator<FaqTranslationInputDto>
{
    public FaqTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .MaximumLength(10)
            .Must(LanguageCodes.IsSupported).WithMessage("Geçersiz dil kodu.");

        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Soru zorunludur.")
            .MaximumLength(300).WithMessage("Soru en fazla 300 karakter olabilir.");

        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("Cevap zorunludur.")
            .MaximumLength(8000).WithMessage("Cevap en fazla 8000 karakter olabilir (HTML formatlamasıyla birlikte).");
    }
}
