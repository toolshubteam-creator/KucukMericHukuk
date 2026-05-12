using FluentValidation;
using KucukMericHukuk.Core.DTOs.Contact;

namespace KucukMericHukuk.Business.Validators;

public class ContactMessageReplyInputValidator : AbstractValidator<ContactMessageReplyInputDto>
{
    public ContactMessageReplyInputValidator()
    {
        RuleFor(x => x.ContactMessageId)
            .GreaterThan(0).WithMessage("Geçerli bir mesaj ID gereklidir.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Yanıt içeriği zorunludur.")
            .MinimumLength(2).WithMessage("Yanıt en az 2 karakter olmalıdır.")
            .MaximumLength(4000).WithMessage("Yanıt en fazla 4000 karakter olabilir.");
    }
}
