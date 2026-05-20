using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Attorney;

namespace KucukMericHukuk.Business.Validators;

public class AttorneyInputValidator : AbstractValidator<AttorneyInputDto>
{
    public AttorneyInputValidator()
    {
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
                .MaximumLength(150);
        });

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber).MaximumLength(50);
        });

        When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl), () =>
        {
            RuleFor(x => x.LinkedInUrl).MaximumLength(500);
        });

        When(x => !string.IsNullOrWhiteSpace(x.BarRegistrationNumber), () =>
        {
            RuleFor(x => x.BarRegistrationNumber).MaximumLength(50);
        });

        When(x => !string.IsNullOrWhiteSpace(x.BarName), () =>
        {
            RuleFor(x => x.BarName).MaximumLength(150);
        });

        When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl), () =>
        {
            RuleFor(x => x.ProfileImageUrl).MaximumLength(500);
        });

        When(x => x.ServiceIds != null && x.ServiceIds.Count > 0, () =>
        {
            RuleForEach(x => x.ServiceIds)
                .GreaterThan(0).WithMessage("Geçersiz hizmet ID.");
        });

        RuleFor(x => x.Translations)
            .Must(list => list != null && list.Any(t => LanguageCodes.IsDefault(t.LanguageCode) && IsActive(t)))
            .WithMessage("Türkçe içerik zorunludur.");

        RuleForEach(x => x.Translations)
            .Where(IsActive)
            .SetValidator(new AttorneyTranslationInputValidator());
    }

    private static bool IsActive(AttorneyTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.FullName)
            || !string.IsNullOrWhiteSpace(t.Title)
            || !string.IsNullOrWhiteSpace(t.Slug)
            || !string.IsNullOrWhiteSpace(t.ShortBio)
            || !string.IsNullOrWhiteSpace(t.FullBio)
            || !string.IsNullOrWhiteSpace(t.Education)
            || !string.IsNullOrWhiteSpace(t.Publications)
            || !string.IsNullOrWhiteSpace(t.MetaTitle)
            || !string.IsNullOrWhiteSpace(t.MetaDescription);
    }
}

public class AttorneyTranslationInputValidator : AbstractValidator<AttorneyTranslationInputDto>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public AttorneyTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.")
            .Must(LanguageCodes.IsSupported).WithMessage("Geçersiz dil kodu.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad Soyad zorunludur.")
            .MaximumLength(150).WithMessage("Ad Soyad en fazla 150 karakter olabilir.");

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("Ünvan en fazla 100 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(200)
                .Matches(SlugRegex).WithMessage("Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        });

        RuleFor(x => x.ShortBio)
            .MaximumLength(500).WithMessage("Kısa biyografi en fazla 500 karakter olabilir.");

        RuleFor(x => x.Education)
            .MaximumLength(2000).WithMessage("Eğitim en fazla 2000 karakter olabilir.");

        RuleFor(x => x.Publications)
            .MaximumLength(2000).WithMessage("Yayınlar en fazla 2000 karakter olabilir.");

        RuleFor(x => x.MetaTitle).MaximumLength(200);
        RuleFor(x => x.MetaDescription).MaximumLength(500);
    }
}
