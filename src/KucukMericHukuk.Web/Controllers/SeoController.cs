using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KucukMericHukuk.Web.Controllers;

/// <summary>
/// SEO endpoint'leri: /sitemap.xml ve /robots.txt
/// Culture prefix YOK — bu dosyalar root-level olmak zorunda (Google standardı).
/// RateLimit muaf (Faz 5.7) — crawler'lar yüksek frekansla tarar.
/// </summary>
[DisableRateLimiting]
public class SeoController : Controller
{
    private readonly ISitemapService _sitemapService;

    public SeoController(ISitemapService sitemapService)
    {
        _sitemapService = sitemapService;
    }

    [HttpGet]
    [Route("sitemap.xml", Order = 0)]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        var xml = await _sitemapService.BuildSitemapXmlAsync(ct);
        return Content(xml, "application/xml", System.Text.Encoding.UTF8);
    }

    [HttpGet]
    [Route("robots.txt", Order = 0)]
    public async Task<IActionResult> Robots(CancellationToken ct)
    {
        var content = await _sitemapService.BuildRobotsTxtAsync(ct);
        return Content(content, "text/plain", System.Text.Encoding.UTF8);
    }
}
