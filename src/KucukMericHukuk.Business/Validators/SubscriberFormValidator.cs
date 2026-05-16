using FluentValidation;
using KucukMericHukuk.Core.DTOs.Subscriber;

namespace KucukMericHukuk.Business.Validators;

public class SubscriberFormValidator : AbstractValidator<SubscriberFormDto>
{
    public SubscriberFormValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.KvkkConsent)
            .Equal(true).WithMessage("Aydınlatma metnini onaylamanız gerekmektedir.");
    }
}
