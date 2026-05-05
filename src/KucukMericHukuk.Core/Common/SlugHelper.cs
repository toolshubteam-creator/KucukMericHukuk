using System.Text;
using System.Text.RegularExpressions;

namespace KucukMericHukuk.Core.Common;

public static class SlugHelper
{
    private static readonly Dictionary<char, string> TurkishMap = new()
    {
        { 'İ', "i" }, { 'I', "i" }, { 'ı', "i" },
        { 'Ş', "s" }, { 'ş', "s" },
        { 'Ğ', "g" }, { 'ğ', "g" },
        { 'Ü', "u" }, { 'ü', "u" },
        { 'Ö', "o" }, { 'ö', "o" },
        { 'Ç', "c" }, { 'ç', "c" },
    };

    private static readonly Regex MultipleHyphens = new("-{2,}", RegexOptions.Compiled);
    private static readonly Regex NonAlphanumeric = new("[^a-z0-9]+", RegexOptions.Compiled);

    public static string Generate(string? input, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            if (TurkishMap.TryGetValue(ch, out var replacement))
            {
                sb.Append(replacement);
            }
            else
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        var lower = sb.ToString();
        var hyphenated = NonAlphanumeric.Replace(lower, "-");
        var collapsed = MultipleHyphens.Replace(hyphenated, "-");
        var trimmed = collapsed.Trim('-');

        if (trimmed.Length > maxLength)
        {
            trimmed = trimmed.Substring(0, maxLength).TrimEnd('-');
        }

        return trimmed;
    }
}
