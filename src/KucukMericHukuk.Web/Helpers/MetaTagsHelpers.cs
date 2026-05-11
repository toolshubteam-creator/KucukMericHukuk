using System.Text;
using System.Text.Encodings.Web;
using KucukMericHukuk.Core.Common;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Web.Helpers;

public static class MetaTagsHelpers
{
    /// <summary>
    /// ViewData'dan meta bilgilerini okur, eksikleri SiteInfo'dan doldurur, head meta tag'lerini render eder.
    /// Layout'ta &lt;head&gt; içinde tek çağrı.
    /// </summary>
    /// <remarks>
    /// View'larda set edilen ViewData anahtarları:
    /// - Title (string?)            — sayfa adı; null ise ana sayfa olarak değerlendirilir
    /// - MetaDescription (string?)  — sayfa-özel description; yoksa SiteInfo.Description
    /// - OgImage (string?)          — relative ya da absolute URL; yoksa SiteInfo.DefaultOgImage
    /// - OgType (string?)           — "website" (default), "article", "profile"
    /// - CanonicalUrl (string?)     — explicit canonical override; yoksa BaseUrl + Request.Path
    /// - MetaRobots (string?)       — "index, follow" (default) / "noindex, nofollow"
    /// </remarks>
    public static IHtmlContent MetaTags(this IHtmlHelper htmlHelper)
    {
        var viewData = htmlHelper.ViewData;
        var ctx = htmlHelper.ViewContext.HttpContext;
        var siteInfo = ctx.RequestServices
            .GetRequiredService<IOptionsSnapshot<SiteInfoOptions>>()
            .Value;

        // Title
        var pageTitle = viewData["Title"] as string;
        var fullTitle = string.IsNullOrEmpty(pageTitle)
            ? (string.IsNullOrEmpty(siteInfo.Tagline)
                ? siteInfo.Name
                : $"{siteInfo.Name} — {siteInfo.Tagline}")
            : $"{pageTitle} | {siteInfo.Name}";

        // Description (fallback: SiteInfo.Description)
        var description = viewData["MetaDescription"] as string;
        if (string.IsNullOrEmpty(description))
            description = siteInfo.Description;

        // OG image (fallback: default; absolute prefix)
        var ogImage = viewData["OgImage"] as string;
        if (string.IsNullOrEmpty(ogImage))
            ogImage = siteInfo.DefaultOgImage;
        if (!string.IsNullOrEmpty(ogImage)
            && !ogImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            ogImage = siteInfo.BaseUrl.TrimEnd('/') + ogImage;
        }

        // OG type
        var ogType = viewData["OgType"] as string ?? "website";

        // Canonical
        var canonicalUrl = viewData["CanonicalUrl"] as string;
        if (string.IsNullOrEmpty(canonicalUrl))
        {
            canonicalUrl = siteInfo.BaseUrl.TrimEnd('/') + ctx.Request.Path;
        }

        // Robots
        var robots = viewData["MetaRobots"] as string ?? "index, follow";

        // OG title fallback (sayfa-özel title yoksa site adı)
        var ogTitle = string.IsNullOrEmpty(pageTitle) ? siteInfo.Name : pageTitle;

        var enc = HtmlEncoder.Default;
        var sb = new StringBuilder();

        sb.AppendLine($"    <title>{enc.Encode(fullTitle)}</title>");
        sb.AppendLine($"    <meta name=\"description\" content=\"{enc.Encode(description)}\" />");
        sb.AppendLine($"    <meta name=\"robots\" content=\"{enc.Encode(robots)}\" />");
        sb.AppendLine($"    <link rel=\"canonical\" href=\"{enc.Encode(canonicalUrl)}\" />");

        // Open Graph
        sb.AppendLine($"    <meta property=\"og:title\" content=\"{enc.Encode(ogTitle)}\" />");
        sb.AppendLine($"    <meta property=\"og:description\" content=\"{enc.Encode(description)}\" />");
        sb.AppendLine($"    <meta property=\"og:type\" content=\"{enc.Encode(ogType)}\" />");
        sb.AppendLine($"    <meta property=\"og:url\" content=\"{enc.Encode(canonicalUrl)}\" />");
        sb.AppendLine($"    <meta property=\"og:image\" content=\"{enc.Encode(ogImage)}\" />");
        sb.AppendLine($"    <meta property=\"og:site_name\" content=\"{enc.Encode(siteInfo.Name)}\" />");
        sb.AppendLine($"    <meta property=\"og:locale\" content=\"{enc.Encode(siteInfo.Locale)}\" />");

        // Twitter Card
        sb.AppendLine($"    <meta name=\"twitter:card\" content=\"summary_large_image\" />");
        sb.AppendLine($"    <meta name=\"twitter:title\" content=\"{enc.Encode(ogTitle)}\" />");
        sb.AppendLine($"    <meta name=\"twitter:description\" content=\"{enc.Encode(description)}\" />");
        sb.AppendLine($"    <meta name=\"twitter:image\" content=\"{enc.Encode(ogImage)}\" />");
        if (!string.IsNullOrEmpty(siteInfo.TwitterHandle))
        {
            sb.AppendLine($"    <meta name=\"twitter:site\" content=\"{enc.Encode(siteInfo.TwitterHandle)}\" />");
        }

        return new HtmlString(sb.ToString());
    }

    /// <summary>
    /// Articles/Index için pagination + kategori filter canonical URL üretir.
    /// View'da: ViewData["CanonicalUrl"] = Html.CanonicalForArticles(Model.SelectedCategorySlug, Model.Articles.PageNumber);
    /// </summary>
    public static string CanonicalForArticles(this IHtmlHelper htmlHelper, string? categorySlug, int pageNumber)
    {
        var ctx = htmlHelper.ViewContext.HttpContext;
        var siteInfo = ctx.RequestServices
            .GetRequiredService<IOptionsSnapshot<SiteInfoOptions>>()
            .Value;

        var basePath = siteInfo.BaseUrl.TrimEnd('/') + ctx.Request.Path;
        var qs = new List<string>();
        if (!string.IsNullOrEmpty(categorySlug))
            qs.Add($"category={Uri.EscapeDataString(categorySlug)}");
        if (pageNumber > 1)
            qs.Add($"page={pageNumber}");

        return qs.Count == 0 ? basePath : basePath + "?" + string.Join("&", qs);
    }
}
