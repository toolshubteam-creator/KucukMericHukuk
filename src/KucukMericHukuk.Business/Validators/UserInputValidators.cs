using FluentValidation;
using KucukMericHukuk.Core.DTOs.User;

namespace KucukMericHukuk.Business.Validators;

public class UserCreateInputValidator : AbstractValidator<UserCreateInputDto>
{
    public UserCreateInputValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.FullName)
            .MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rol seçimi zorunludur.");
    }
}

public class UserUpdateInputValidator : AbstractValidator<UserUpdateInputDto>
{
    public UserUpdateInputValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.FullName)
            .MaximumLength(150);

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rol seçimi zorunludur.");
    }
}

public class ResetPasswordInputValidator : AbstractValidator<ResetPasswordInputDto>
{
    public ResetPasswordInputValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");
    }
}
