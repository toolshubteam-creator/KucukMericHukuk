using System.Text.RegularExpressions;
using FluentValidation;
using KucukMericHukuk.Core.DTOs.Service;

namespace KucukMericHukuk.Business.Validators;

public class ServiceInputValidator : AbstractValidator<ServiceInputDto>
{
    public ServiceInputValidator()
    {
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıra numarası 0 veya pozitif olmalıdır.");

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("Icon adı en fazla 50 karakter olabilir.");

        RuleFor(x => x.FeaturedImage)
            .MaximumLength(500).WithMessage("Öne çıkan görsel yolu en fazla 500 karakter olabilir.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("En az bir dil için içerik girilmelidir.");

        RuleForEach(x => x.Translations).SetValidator(new ServiceTranslationInputValidator());

        When(x => x.AttorneyIds != null && x.AttorneyIds.Count > 0, () =>
        {
            RuleForEach(x => x.AttorneyIds)
                .GreaterThan(0).WithMessage("Geçersiz avukat ID.");
        });
    }
}

public class ServiceTranslationInputValidator : AbstractValidator<ServiceTranslationInputDto>
{
    private static readonly Regex SlugRegex = new(
        "^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public ServiceTranslationInputValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Dil kodu zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hizmet adı zorunludur.")
            .MaximumLength(150).WithMessage("Hizmet adı en fazla 150 karakter olabilir.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(200).WithMessage("Slug en fazla 200 karakter olabilir.")
                .Matches(SlugRegex).WithMessage(
                    "Slug yalnızca küçük harf, rakam ve tire içerebilir.");
        });

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500).WithMessage("Kısa açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.FullDescription)
            .NotEmpty().WithMessage("Detaylı açıklama zorunludur.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta açıklama en fazla 500 karakter olabilir.");
    }
}
