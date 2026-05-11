using FluentValidation;
using KucukMericHukuk.Core.DTOs.Faq;

namespace KucukMericHukuk.Business.Validators;

public class FaqInputValidator : AbstractValidator<FaqInputDto>
{
    public FaqInputValidator()
    {
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("En az bir dil için içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new FaqTranslationInputValidator());
    }
}

public class FaqTranslationInputValidator : AbstractValidator<FaqTranslationInputDto>
{
    public FaqTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .MaximumLength(10);

        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Soru zorunludur.")
            .MaximumLength(300).WithMessage("Soru en fazla 300 karakter olabilir.");

        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("Cevap zorunludur.")
            .MaximumLength(2000).WithMessage("Cevap en fazla 2000 karakter olabilir.");
    }
}
