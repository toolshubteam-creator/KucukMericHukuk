namespace KucukMericHukuk.Core.Common;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
            throw new InvalidOperationException("Başarılı sonuçta hata listesi boş olmalı.");
        if (!isSuccess && errors.Count == 0)
            throw new InvalidOperationException("Başarısız sonuçta en az bir hata olmalı.");

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    public Error? FirstError => Errors.Count > 0 ? Errors[0] : null;

    public static Result Success() => new(true, Array.Empty<Error>());

    public static Result Failure(Error error) =>
        new(false, new[] { error });

    public static Result Failure(IEnumerable<Error> errors) =>
        new(false, errors.ToArray());

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static Result<T> Failure<T>(IEnumerable<Error> errors) => Result<T>.Failure(errors);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, Array.Empty<Error>())
    {
        _value = value;
    }

    private Result(IReadOnlyList<Error> errors) : base(false, errors) { }

    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException(
                    "Başarısız Result'tan Value okunamaz. Önce IsSuccess kontrol edin.");
            return _value!;
        }
    }

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(new[] { error });

    public static new Result<T> Failure(IEnumerable<Error> errors) => new(errors.ToArray());
}
