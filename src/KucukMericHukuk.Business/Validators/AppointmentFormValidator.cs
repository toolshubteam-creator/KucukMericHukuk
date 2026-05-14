using FluentValidation;
using KucukMericHukuk.Core.DTOs.Appointment;

namespace KucukMericHukuk.Business.Validators;

public class AppointmentFormValidator : AbstractValidator<AppointmentFormDto>
{
    public AppointmentFormValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ad Soyad zorunludur.")
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon zorunludur.")
            .MaximumLength(50)
            .Matches(@"^[\d\s\+\-\(\)]*$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Telefon yalnızca rakam, boşluk ve +-() karakterleri içerebilir.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Konu zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.PreferredDate)
            .NotNull().WithMessage("Tercih edilen tarih zorunludur.")
            .Must(d => d!.Value >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
                .When(x => x.PreferredDate.HasValue)
                .WithMessage("Tercih edilen tarih geçmişte olamaz.");

        RuleFor(x => x.PreferredTimeNote)
            .MaximumLength(100);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);

        RuleFor(x => x.KvkkConsent)
            .Equal(true).WithMessage("Aydınlatma metnini onaylamanız gerekmektedir.");
    }
}
