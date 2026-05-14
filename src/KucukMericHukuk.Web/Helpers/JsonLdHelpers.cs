using KucukMericHukuk.Web.Middleware;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KucukMericHukuk.Web.Helpers;

public static class JsonLdHelpers
{
    /// <summary>
    /// JSON string'i &lt;script type="application/ld+json"&gt; tag'i içinde render eder.
    /// JSON içeriği zaten escape edilmiş (JsonSerializer çıktısı), Razor encoding atlanır.
    /// Faz 6.20: CSP script-src nonce'lu — application/ld+json blokları çalışmasa da
    /// tarayıcı script-src'yi onlara da uygular; nonce yoksa CSP bloklar (JSON-LD SEO kaybı).
    /// </summary>
    public static IHtmlContent JsonLd(this IHtmlHelper htmlHelper, string jsonLd)
    {
        if (string.IsNullOrWhiteSpace(jsonLd))
            return HtmlString.Empty;

        var nonce = htmlHelper.ViewContext.HttpContext.Items[SecurityHeadersMiddleware.NonceItemKey] as string;
        var nonceAttr = string.IsNullOrEmpty(nonce) ? "" : $" nonce=\"{nonce}\"";

        return new HtmlString(
            $"<script type=\"application/ld+json\"{nonceAttr}>{jsonLd}</script>");
    }
}
