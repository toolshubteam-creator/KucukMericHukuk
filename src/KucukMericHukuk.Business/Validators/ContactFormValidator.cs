using FluentValidation;
using KucukMericHukuk.Core.DTOs.Contact;

namespace KucukMericHukuk.Business.Validators;

public class ContactFormValidator : AbstractValidator<ContactFormDto>
{
    public ContactFormValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ad Soyad zorunludur.")
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .Matches(@"^[\d\s\+\-\(\)]*$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Telefon yalnızca rakam, boşluk ve +-() karakterleri içerebilir.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Konu zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mesaj zorunludur.")
            .MinimumLength(10).WithMessage("Mesaj en az 10 karakter olmalıdır.");

        RuleFor(x => x.KvkkConsent)
            .Equal(true).WithMessage("Aydınlatma metnini onaylamanız gerekmektedir.");
    }
}
