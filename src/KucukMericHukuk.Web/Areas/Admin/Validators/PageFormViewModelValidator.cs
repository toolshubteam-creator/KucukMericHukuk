using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Pages;

namespace KucukMericHukuk.Web.Areas.Admin.Validators;

// NOT: Kurallar Business/Validators/PageInputValidator.cs'ten ayna kopya.
// Form-level validator client-side data-val-* attribute üretimi için.
// Server-side validation hâlâ service katmanında PageInputValidator çalıştırır.
public class PageFormViewModelValidator : AbstractValidator<PageFormViewModel>
{
    private static readonly Regex PageKeyRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public PageFormViewModelValidator()
    {
        RuleFor(x => x.PageKey)
            .NotEmpty().WithMessage("Sayfa anahtarı (PageKey) zorunludur.")
            .MaximumLength(100).WithMessage("Sayfa anahtarı en fazla 100 karakter olabilir.")
            .Matches(PageKeyRegex).WithMessage(
                "Sayfa anahtarı yalnızca küçük harf, rakam ve tire içerebilir (örn. 'hakkimizda').");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("En az bir dil için içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new PageTranslationFormViewModelValidator());
    }
}

public class PageTranslationFormViewModelValidator : AbstractValidator<PageTranslationFormViewModel>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public PageTranslationFormViewModelValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik zorunludur.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug!)
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
