namespace KucukMericHukuk.Core.Common;

public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public string? Field { get; }

    public Error(string code, string message, string? field = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Error code boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message boş olamaz.", nameof(message));

        Code = code;
        Message = message;
        Field = field;
    }

    public static Error None => new("None", "Hata yok.");

    public override string ToString() =>
        Field is null ? $"[{Code}] {Message}" : $"[{Code}] ({Field}) {Message}";
}
