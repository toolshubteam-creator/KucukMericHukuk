using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KucukMericHukuk.Web.Helpers;

public static class HtmlContentHelpers
{
    private static readonly Regex ImgTagRegex = new(
        @"<img\s+(?![^>]*\bloading\s*=)([^>]*?)>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IHtmlContent LazyImageHtml(this IHtmlHelper htmlHelper, string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return HtmlString.Empty;
        }
        var enriched = ImgTagRegex.Replace(content, "<img loading=\"lazy\" $1>");
        return new HtmlString(enriched);
    }
}
