using System.Text;
using System.Xml.Linq;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Seo;

public class SitemapService : ISitemapService
{
    private const string DefaultLanguageCode = "tr-TR";
    private static readonly XNamespace _sitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly SiteInfoOptions _siteInfo;
    private readonly IArticleRepository _articles;
    private readonly IServiceRepository _services;
    private readonly IAttorneyRepository _attorneys;
    private readonly IPageRepository _pages;

    public SitemapService(
        IOptionsSnapshot<SiteInfoOptions> siteInfo,
        IArticleRepository articles,
        IServiceRepository services,
        IAttorneyRepository attorneys,
        IPageRepository pages)
    {
        _siteInfo = siteInfo.Value;
        _articles = articles;
        _services = services;
        _attorneys = attorneys;
        _pages = pages;
    }

    private string BaseUrl => _siteInfo.BaseUrl.TrimEnd('/');
    private string CulturePrefix => $"/{DefaultLanguageCode}";

    public async Task<string> BuildSitemapXmlAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var urlset = new XElement(_sitemapNs + "urlset");

        // 1) Ana sayfa
        urlset.Add(BuildUrlElement($"{BaseUrl}{CulturePrefix}/", today));

        // 2) Statik liste sayfaları
        var staticPaths = new[]
        {
            "/Articles",
            "/Services",
            "/Attorneys",
            "/Faqs",
            "/Contact"
        };
        foreach (var p in staticPaths)
            urlset.Add(BuildUrlElement($"{BaseUrl}{CulturePrefix}{p}", today));

        // 3) Dinamik Page'ler (about, privacy, cookie, terms vb.)
        var pages = await _pages.GetAllActiveForSitemapAsync(DefaultLanguageCode, ct);
        foreach (var page in pages)
        {
            var slug = page.Translations.FirstOrDefault(t => t.LanguageCode == DefaultLanguageCode)?.Slug;
            if (string.IsNullOrEmpty(slug)) continue;
            var lastmod = (page.UpdatedAt ?? page.CreatedAt).ToString("yyyy-MM-dd");
            urlset.Add(BuildUrlElement($"{BaseUrl}{CulturePrefix}/Pages/{slug}", lastmod));
        }

        // 4) Articles
        var articles = await _articles.GetAllPublishedForSitemapAsync(DefaultLanguageCode, ct);
        foreach (var article in articles)
        {
            var slug = article.Translations.FirstOrDefault(t => t.LanguageCode == DefaultLanguageCode)?.Slug;
            if (string.IsNullOrEmpty(slug)) continue;
            var lastmod = (article.UpdatedAt ?? article.PublishedAt ?? article.CreatedAt).ToString("yyyy-MM-dd");
            urlset.Add(BuildUrlElement($"{BaseUrl}{CulturePrefix}/Articles/{slug}", lastmod));
        }

        // 5) Services
        var services = await _services.GetActiveOrderedAsync(DefaultLanguageCode, ct);
        foreach (var service in services)
        {
            var slug = service.Translations.FirstOrDefault(t => t.LanguageCode == DefaultLanguageCode)?.Slug;
            if (string.IsNullOrEmpty(slug)) continue;
            var lastmod = (service.UpdatedAt ?? service.CreatedAt).ToString("yyyy-MM-dd");
            urlset.Add(BuildUrlElement($"{BaseUrl}{CulturePrefix}/Services/{slug}", lastmod));
        }

        // 6) Attorneys
        var attorneys = await _attorneys.GetActiveOrderedAsync(DefaultLanguageCode, ct);
        foreach (var attorney in attorneys)
        {
            var slug = attorney.Translations.FirstOrDefault(t => t.LanguageCode == DefaultLanguageCode)?.Slug;
            if (string.IsNullOrEmpty(slug)) continue;
            var lastmod = (attorney.UpdatedAt ?? attorney.CreatedAt).ToString("yyyy-MM-dd");
            urlset.Add(BuildUrlElement($"{BaseUrl}{CulturePrefix}/Attorneys/{slug}", lastmod));
        }

        var xdoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            urlset);

        using var sw = new Utf8StringWriter();
        xdoc.Save(sw);
        return sw.ToString();
    }

    public string BuildRobotsTxt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Disallow: /admin/");
        sb.AppendLine("Disallow: /Identity/");
        sb.AppendLine("Disallow: /Contact/ThankYou");
        sb.AppendLine("Disallow: /Error/");
        sb.AppendLine("Allow: /");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {BaseUrl}/sitemap.xml");
        return sb.ToString();
    }

    private XElement BuildUrlElement(string loc, string lastmod)
    {
        return new XElement(_sitemapNs + "url",
            new XElement(_sitemapNs + "loc", loc),
            new XElement(_sitemapNs + "lastmod", lastmod));
    }

    /// <summary>UTF-8 encoding ile XML üretmek için (StringWriter default UTF-16 yazıyor).</summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
