namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// Sitemap.xml ve robots.txt içerik üreticisi.
/// </summary>
public interface ISitemapService
{
    /// <summary>
    /// Tüm public, indexable URL'leri içeren sitemap.xml stringini üretir.
    /// </summary>
    Task<string> BuildSitemapXmlAsync(CancellationToken ct = default);

    /// <summary>
    /// robots.txt içeriğini üretir (sitemap referansı dahil).
    /// Önce SiteSettings (Seo.RobotsTxt) DB değerine bakar; boş/null ise hardcoded fallback döner.
    /// </summary>
    Task<string> BuildRobotsTxtAsync(CancellationToken ct = default);
}
