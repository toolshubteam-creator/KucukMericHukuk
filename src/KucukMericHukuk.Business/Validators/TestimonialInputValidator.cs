using FluentValidation;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Testimonial;

namespace KucukMericHukuk.Business.Validators;

public class TestimonialInputValidator : AbstractValidator<TestimonialInputDto>
{
    public TestimonialInputValidator()
    {
        // TBB reklam yasağı: inisyal/rumuz max 10 char (örn. "M.A.")
        RuleFor(x => x.AuthorInitials)
            .MaximumLength(10).WithMessage("İnisyal en fazla 10 karakter olabilir (örn. \"M.A.\").");

        RuleFor(x => x.AuthorRole)
            .MaximumLength(100).WithMessage("Rol en fazla 100 karakter olabilir.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).When(x => x.Rating.HasValue)
            .WithMessage("Yıldız puanı 1 ile 5 arasında olmalıdır.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("En az bir dil için içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new TestimonialTranslationInputValidator());
    }
}

public class TestimonialTranslationInputValidator : AbstractValidator<TestimonialTranslationInputDto>
{
    public TestimonialTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .MaximumLength(10)
            .Must(code => LanguageCodes.Supported.Contains(code))
            .WithMessage("Desteklenmeyen dil kodu.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Yorum içeriği zorunludur.")
            .MinimumLength(10).WithMessage("Yorum en az 10 karakter olmalıdır.")
            .MaximumLength(2000).WithMessage("Yorum en fazla 2000 karakter olabilir.");
    }
}
