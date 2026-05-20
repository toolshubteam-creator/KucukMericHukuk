namespace KucukMericHukuk.Core.Constants;

public static class LanguageCodes
{
    public const string Turkish = "tr-TR";
    public const string English = "en-US";
    public const string German = "de-DE";

    public const string Default = Turkish;

    public static readonly string[] Supported = new[] { Turkish, English };

    public static bool IsDefault(string languageCode)
    {
        return string.Equals(languageCode, Default, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSupported(string languageCode)
    {
        return Supported.Contains(languageCode, StringComparer.OrdinalIgnoreCase);
    }
}
