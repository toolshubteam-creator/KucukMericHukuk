using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Core.Routing;
using KucukMericHukuk.Infrastructure.Email.Templates;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Newsletter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/newsletter")]
[Authorize(Roles = "Admin")]
public class NewsletterController : Controller
{
    private const int HistoryPageSize = 20;

    private readonly INewsletterService _newsletterService;
    private readonly INewsletterDispatcher _dispatcher;
    private readonly IUnitOfWork _uow;
    private readonly SiteInfoOptions _siteInfo;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(
        INewsletterService newsletterService,
        INewsletterDispatcher dispatcher,
        IUnitOfWork uow,
        IOptionsSnapshot<SiteInfoOptions> siteInfoOptions,
        ILogger<NewsletterController> logger)
    {
        _newsletterService = newsletterService;
        _dispatcher = dispatcher;
        _uow = uow;
        _siteInfo = siteInfoOptions.Value;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index(int historyPage = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Bülten";

        var pendingResult = await _newsletterService.GetPendingArticlesAsync(ct);
        var historyResult = await _newsletterService.GetJobHistoryAsync(historyPage, HistoryPageSize, ct);

        if (pendingResult.IsFailure || historyResult.IsFailure)
        {
            TempData["Error"] = "Bülten verileri yüklenirken bir sorun oluştu.";
            return RedirectToAction("Index", "Admin");
        }

        var vm = new NewsletterIndexViewModel
        {
            Pending = pendingResult.Value,
            History = historyResult.Value
        };

        return View(vm);
    }

    [HttpGet("preview/{articleId:int}")]
    public async Task<IActionResult> Preview(int articleId, CancellationToken ct)
    {
        ViewData["Title"] = "Bülten Önizleme";

        var article = await _uow.Articles.GetByIdWithDetailsAsync(articleId, LanguageCodes.Default, ct);
        if (article is null)
        {
            return NotFound();
        }

        var translation = article.Translations.FirstOrDefault();
        var slug = translation?.Slug ?? string.Empty;
        var baseUrl = _siteInfo.BaseUrl.TrimEnd('/');

        var model = new NewsletterEmailModel(
            ArticleTitle: translation?.Title ?? "(başlık yok)",
            ArticleExcerpt: translation?.Excerpt,
            FeaturedImageUrl: ComposeAbsoluteUrl(article.FeaturedImageUrl, baseUrl),
            ArticleUrl: $"{baseUrl}{PublicRouteSegments.ArticleDetailPath(LanguageCodes.Default, slug)}",
            UnsubscribeUrl: $"{baseUrl}{PublicRouteSegments.UnsubscribePath(LanguageCodes.Default, Guid.Empty)}",
            SiteName: _siteInfo.Name);

        var html = NewsletterEmailTemplate.Render(model);
        return Content(html, "text/html");
    }

    [HttpPost("create-job/{articleId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJob(int articleId, CancellationToken ct)
    {
        var result = await _newsletterService.CreateJobAsync(articleId, ct);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Newsletter job olusturulamadi: articleId={ArticleId}, errors={Errors}",
                articleId, string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = result.FirstError?.Message ?? "Bülten kaydı oluşturulamadı.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Bülten kaydı oluşturuldu (Pending). 'Gönder' butonuyla aboneye iletilebilir.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Faz 7.2b-2: Pending job'u arka plan gönderim için dispatcher'a teslim eder.
    /// Job zaten Sending/Completed/Failed ise reddeder. Controller hemen döner —
    /// kullanıcı durum geçişini Gönderim Geçmişi tablosunu yenileyerek izler.
    /// </summary>
    [HttpPost("send-job/{jobId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendJob(int jobId, CancellationToken ct)
    {
        var jobResult = await _newsletterService.GetJobByIdAsync(jobId, ct);
        if (jobResult.IsFailure)
        {
            if (jobResult.FirstError?.Code == ErrorCodes.Newsletter.JobNotFound)
                return NotFound();
            TempData["Error"] = jobResult.FirstError?.Message ?? "Job bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (jobResult.Value.Status != NewsletterJobStatus.Pending)
        {
            TempData["Error"] = $"Sadece Pending job gönderilebilir. Mevcut durum: {jobResult.Value.Status}.";
            return RedirectToAction(nameof(Index));
        }

        _dispatcher.Dispatch(jobId);
        TempData["Success"] = "Bülten gönderimi başlatıldı. Durumu Gönderim Geçmişi tablosundan izleyebilirsiniz.";
        return RedirectToAction(nameof(Index));
    }

    private static string? ComposeAbsoluteUrl(string? relativeOrAbsolute, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;
        if (relativeOrAbsolute.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeOrAbsolute.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relativeOrAbsolute;
        return $"{baseUrl}/{relativeOrAbsolute.TrimStart('/')}";
    }
}
