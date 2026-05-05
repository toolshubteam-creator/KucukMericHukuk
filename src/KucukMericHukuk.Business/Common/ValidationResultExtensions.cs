using FluentValidation.Results;
using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Business.Common;

public static class ValidationResultExtensions
{
    public static Result<T> ToFailureResult<T>(this ValidationResult vr)
    {
        if (vr.IsValid)
            throw new InvalidOperationException(
                "Geçerli ValidationResult Failure'a çevrilemez.");

        var errors = vr.Errors
            .Select(e => new Error(
                code: !string.IsNullOrWhiteSpace(e.ErrorCode) ? e.ErrorCode : ErrorCodes.Common.Validation,
                message: e.ErrorMessage,
                field: e.PropertyName))
            .ToArray();

        return Result<T>.Failure(errors);
    }

    public static Result ToFailureResult(this ValidationResult vr)
    {
        if (vr.IsValid)
            throw new InvalidOperationException(
                "Geçerli ValidationResult Failure'a çevrilemez.");

        var errors = vr.Errors
            .Select(e => new Error(
                code: !string.IsNullOrWhiteSpace(e.ErrorCode) ? e.ErrorCode : ErrorCodes.Common.Validation,
                message: e.ErrorMessage,
                field: e.PropertyName))
            .ToArray();

        return Result.Failure(errors);
    }
}
