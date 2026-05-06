using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Attorneys;

namespace KucukMericHukuk.Web.Areas.Admin.Validators;

// NOT: Kurallar Business/Validators/AttorneyInputValidator.cs'ten ayna kopya.
public class AttorneyFormViewModelValidator : AbstractValidator<AttorneyFormViewModel>
{
    public AttorneyFormViewModelValidator()
    {
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!)
                .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
                .MaximumLength(150);
        });

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .MaximumLength(50);
        });

        When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl), () =>
        {
            RuleFor(x => x.LinkedInUrl!)
                .MaximumLength(500);
        });

        When(x => !string.IsNullOrWhiteSpace(x.BarRegistrationNumber), () =>
        {
            RuleFor(x => x.BarRegistrationNumber!)
                .MaximumLength(50);
        });

        When(x => !string.IsNullOrWhiteSpace(x.BarName), () =>
        {
            RuleFor(x => x.BarName!)
                .MaximumLength(150);
        });

        When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl), () =>
        {
            RuleFor(x => x.ProfileImageUrl!)
                .MaximumLength(500);
        });

        When(x => x.ServiceIds != null && x.ServiceIds.Count > 0, () =>
        {
            RuleForEach(x => x.ServiceIds)
                .GreaterThan(0).WithMessage("Geçersiz hizmet ID.");
        });

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("En az bir dil için içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new AttorneyTranslationFormViewModelValidator());
    }
}

public class AttorneyTranslationFormViewModelValidator : AbstractValidator<AttorneyTranslationFormViewModel>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public AttorneyTranslationFormViewModelValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad Soyad zorunludur.")
            .MaximumLength(150).WithMessage("Ad Soyad en fazla 150 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Title), () =>
        {
            RuleFor(x => x.Title!)
                .MaximumLength(100).WithMessage("Ünvan en fazla 100 karakter olabilir.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug!)
                .MaximumLength(200)
                .Matches(SlugRegex).WithMessage(
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ShortBio), () =>
        {
            RuleFor(x => x.ShortBio!)
                .MaximumLength(500).WithMessage("Kısa biyografi en fazla 500 karakter olabilir.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Education), () =>
        {
            RuleFor(x => x.Education!)
                .MaximumLength(2000).WithMessage("Eğitim en fazla 2000 karakter olabilir.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Publications), () =>
        {
            RuleFor(x => x.Publications!)
                .MaximumLength(2000).WithMessage("Yayınlar en fazla 2000 karakter olabilir.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.MetaTitle), () =>
        {
            RuleFor(x => x.MetaTitle!)
                .MaximumLength(200);
        });

        When(x => !string.IsNullOrWhiteSpace(x.MetaDescription), () =>
        {
            RuleFor(x => x.MetaDescription!)
                .MaximumLength(500);
        });
    }
}
