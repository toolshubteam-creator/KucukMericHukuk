using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Seo;

public class JsonLdService : IJsonLdService
{
    private readonly SiteInfoOptions _siteInfo;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public JsonLdService(IOptionsSnapshot<SiteInfoOptions> siteInfo)
    {
        _siteInfo = siteInfo.Value;
    }

    private string BaseUrl => _siteInfo.BaseUrl.TrimEnd('/');

    private string AbsoluteUrl(string? path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        return BaseUrl + (path.StartsWith("/") ? path : "/" + path);
    }

    public string BuildLegalService()
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "LegalService",
            ["name"] = _siteInfo.Name,
            ["url"] = BaseUrl,
            ["description"] = NullIfEmpty(_siteInfo.Description),
            ["image"] = AbsoluteUrl(_siteInfo.DefaultOgImage),
            ["telephone"] = NullIfEmpty(_siteInfo.Telephone),
            ["email"] = NullIfEmpty(_siteInfo.Email),
            ["areaServed"] = NullIfEmpty(_siteInfo.AreaServed),
            ["address"] = BuildPostalAddress(),
            ["geo"] = BuildGeoCoordinates(),
            ["openingHours"] = NullIfEmpty(_siteInfo.OpeningHoursDescription)
        };

        return SerializeWithoutNulls(schema);
    }

    public string BuildWebSite()
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = _siteInfo.Name,
            ["url"] = BaseUrl,
            ["inLanguage"] = "tr-TR",
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@type"] = "LegalService",
                ["name"] = _siteInfo.Name
            }
        };
        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    public string BuildArticle(ArticleDetailDto article)
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = article.Title,
            ["description"] = NullIfEmpty(article.MetaDescription) ?? NullIfEmpty(article.Excerpt),
            ["image"] = NullIfEmpty(AbsoluteUrl(article.FeaturedImageUrl))
                ?? AbsoluteUrl(_siteInfo.DefaultOgImage),
            ["datePublished"] = article.PublishedAt?.ToString("o"),
            ["dateModified"] = article.PublishedAt?.ToString("o"),
            ["author"] = !string.IsNullOrEmpty(article.AuthorName)
                ? new Dictionary<string, object?>
                {
                    ["@type"] = "Person",
                    ["name"] = article.AuthorName
                }
                : null,
            ["editor"] = !string.IsNullOrEmpty(article.EditorName)
                ? new Dictionary<string, object?>
                {
                    ["@type"] = "Person",
                    ["name"] = article.EditorName
                }
                : null,
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@type"] = "LegalService",
                ["name"] = _siteInfo.Name,
                ["logo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = AbsoluteUrl(_siteInfo.DefaultOgImage)
                }
            },
            ["mainEntityOfPage"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebPage",
                ["@id"] = $"{BaseUrl}/tr-TR/Articles/{article.Slug}"
            }
        };
        return SerializeWithoutNulls(schema);
    }

    public string BuildPerson(AttorneyDetailDto attorney)
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Person",
            ["name"] = attorney.FullName,
            ["jobTitle"] = NullIfEmpty(attorney.Title) ?? "Avukat",
            ["description"] = NullIfEmpty(attorney.MetaDescription) ?? NullIfEmpty(attorney.ShortBio),
            ["image"] = NullIfEmpty(AbsoluteUrl(attorney.ProfileImageUrl)),
            ["url"] = $"{BaseUrl}/tr-TR/Attorneys/{attorney.Slug}",
            ["email"] = NullIfEmpty(attorney.Email),
            ["telephone"] = NullIfEmpty(attorney.PhoneNumber),
            ["sameAs"] = string.IsNullOrEmpty(attorney.LinkedInUrl)
                ? null
                : new[] { attorney.LinkedInUrl },
            ["worksFor"] = new Dictionary<string, object?>
            {
                ["@type"] = "LegalService",
                ["name"] = _siteInfo.Name,
                ["url"] = BaseUrl
            }
        };
        return SerializeWithoutNulls(schema);
    }

    public string BuildBreadcrumbList(IEnumerable<(string Label, string? Url)> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            return string.Empty;

        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = itemList.Select((item, index) =>
            {
                var listItem = new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = index + 1,
                    ["name"] = item.Label
                };
                if (!string.IsNullOrEmpty(item.Url))
                {
                    var absoluteUrl = item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? item.Url
                        : BaseUrl + (item.Url.StartsWith("/") ? item.Url : "/" + item.Url);
                    listItem["item"] = absoluteUrl;
                }
                return listItem;
            }).ToArray()
        };

        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    public string BuildFaqPage(IEnumerable<FaqListDto> faqs)
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = faqs.Select(f => new Dictionary<string, object?>
            {
                ["@type"] = "Question",
                ["name"] = f.Question,
                ["acceptedAnswer"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Answer",
                    // FAQPage schema plain text bekler. HTML (Quill çıktısı) strip edilir,
                    // entity'ler decode edilir, çoklu whitespace tek boşluğa indirilir.
                    ["text"] = StripHtml(f.Answer)
                }
            }).ToArray()
        };
        return JsonSerializer.Serialize(schema, _jsonOptions);
    }

    // ── Helpers ──

    private static string SerializeWithoutNulls(Dictionary<string, object?> schema)
    {
        var filtered = schema.Where(kv => kv.Value != null)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return JsonSerializer.Serialize(filtered, _jsonOptions);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// HTML tag'lerini siler, entity'leri decode eder, çoklu whitespace'i tek boşluğa indirir.
    /// JSON-LD schema (FAQPage, Article) plain text bekler.
    /// </summary>
    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var withoutTags = HtmlTagRegex.Replace(html, " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex.Replace(decoded, " ").Trim();
    }

    private Dictionary<string, object?>? BuildPostalAddress()
    {
        if (string.IsNullOrEmpty(_siteInfo.StreetAddress)
            && string.IsNullOrEmpty(_siteInfo.AddressLocality))
            return null;

        var address = new Dictionary<string, object?>
        {
            ["@type"] = "PostalAddress",
            ["streetAddress"] = NullIfEmpty(_siteInfo.StreetAddress),
            ["addressLocality"] = NullIfEmpty(_siteInfo.AddressLocality),
            ["addressRegion"] = NullIfEmpty(_siteInfo.AddressRegion),
            ["postalCode"] = NullIfEmpty(_siteInfo.PostalCode),
            ["addressCountry"] = NullIfEmpty(_siteInfo.AddressCountry)
        };
        return address.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private Dictionary<string, object?>? BuildGeoCoordinates()
    {
        if (!_siteInfo.Latitude.HasValue || !_siteInfo.Longitude.HasValue)
            return null;

        return new Dictionary<string, object?>
        {
            ["@type"] = "GeoCoordinates",
            ["latitude"] = _siteInfo.Latitude.Value,
            ["longitude"] = _siteInfo.Longitude.Value
        };
    }
}
