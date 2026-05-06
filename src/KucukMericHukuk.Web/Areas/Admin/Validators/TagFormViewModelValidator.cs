using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Tags;

namespace KucukMericHukuk.Web.Areas.Admin.Validators;

// NOT: Kurallar Business/Validators/TagInputValidator.cs'ten ayna kopya.
public class TagFormViewModelValidator : AbstractValidator<TagFormViewModel>
{
    public TagFormViewModelValidator()
    {
        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("En az bir dil için içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new TagTranslationFormViewModelValidator());
    }
}

public class TagTranslationFormViewModelValidator : AbstractValidator<TagTranslationFormViewModel>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public TagTranslationFormViewModelValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Etiket adı zorunludur.")
            .MaximumLength(50).WithMessage("Etiket adı en fazla 50 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug!)
                .MaximumLength(50).WithMessage("Slug en fazla 50 karakter olabilir.")
                .Matches(SlugRegex).WithMessage(
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir (örn. 'icra-hukuku').");
        });
    }
}
