using FluentValidation;
using KucukMericHukuk.Core.DTOs.Media;

namespace KucukMericHukuk.Business.Validators;

public class MediaUploadInputValidator : AbstractValidator<MediaUploadInputDto>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB (CLAUDE.md §8)
    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/webp" };

    public MediaUploadInputValidator()
    {
        RuleFor(x => x.OriginalFileName)
            .NotEmpty().WithMessage("Dosya adı zorunludur.")
            .MaximumLength(500);

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("İçerik türü zorunludur.")
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Sadece JPEG, PNG ve WebP formatları desteklenir.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("Dosya boş olamaz.")
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("Dosya boyutu en fazla 10 MB olabilir.");

        RuleFor(x => x.AltText)
            .MaximumLength(500);
    }
}

public class MediaUpdateInputValidator : AbstractValidator<MediaUpdateInputDto>
{
    public MediaUpdateInputValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AltText).MaximumLength(500);
    }
}
