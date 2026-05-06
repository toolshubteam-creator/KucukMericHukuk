using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Categories;

namespace KucukMericHukuk.Web.Areas.Admin.Validators;

// NOT: Kurallar Business/Validators/CategoryInputValidator.cs'ten ayna kopya.
public class CategoryFormViewModelValidator : AbstractValidator<CategoryFormViewModel>
{
    public CategoryFormViewModelValidator()
    {
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        // Self-reference guard (Id == ParentCategoryId yasak)
        RuleFor(x => x).Must(x =>
            !x.Id.HasValue || x.ParentCategoryId != x.Id.Value)
            .WithMessage("Kategori kendi üst kategorisi olamaz.")
            .OverridePropertyName(nameof(CategoryFormViewModel.ParentCategoryId));

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("En az bir dil için içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new CategoryTranslationFormViewModelValidator());
    }
}

public class CategoryTranslationFormViewModelValidator : AbstractValidator<CategoryTranslationFormViewModel>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public CategoryTranslationFormViewModelValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug!)
                .MaximumLength(150).WithMessage("Slug en fazla 150 karakter olabilir.")
                .Matches(SlugRegex).WithMessage(
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        });

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta açıklama en fazla 500 karakter olabilir.");
    }
}
