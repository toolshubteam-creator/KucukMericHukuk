using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

/// <summary>
/// SEO endpoint'leri: /sitemap.xml ve /robots.txt
/// Culture prefix YOK — bu dosyalar root-level olmak zorunda (Google standardı).
/// </summary>
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
    public IActionResult Robots()
    {
        var content = _sitemapService.BuildRobotsTxt();
        return Content(content, "text/plain", System.Text.Encoding.UTF8);
    }
}
