namespace KucukMericHukuk.Core.Constants;

public static class LanguageCodes
{
    public const string Turkish = "tr-TR";
    public const string English = "en-US";
    public const string German = "de-DE";

    public const string Default = Turkish;

    public static readonly string[] Supported = new[] { Turkish };
    // İleride: new[] { Turkish, English, German }
}
