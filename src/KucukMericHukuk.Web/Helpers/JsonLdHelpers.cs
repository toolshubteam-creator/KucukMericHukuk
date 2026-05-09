using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KucukMericHukuk.Web.Helpers;

public static class JsonLdHelpers
{
    /// <summary>
    /// JSON string'i &lt;script type="application/ld+json"&gt; tag'i içinde render eder.
    /// JSON içeriği zaten escape edilmiş (JsonSerializer çıktısı), Razor encoding atlanır.
    /// </summary>
    public static IHtmlContent JsonLd(this IHtmlHelper htmlHelper, string jsonLd)
    {
        if (string.IsNullOrWhiteSpace(jsonLd))
            return HtmlString.Empty;

        return new HtmlString(
            $"<script type=\"application/ld+json\">{jsonLd}</script>");
    }
}
