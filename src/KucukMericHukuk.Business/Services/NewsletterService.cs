using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Newsletter;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Core.Routing;
using KucukMericHukuk.Infrastructure.Email.Templates;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Business.Services;

public class NewsletterService : INewsletterService
{
    // Faz 7.2b-2: batch parametreleri — SMTP rate limit + DB ilerleme yazma frekansı
    private const int BatchSize = 10;
    private const int BatchDelayMs = 1500;
    private const int MaxErrorSamplesInSummary = 5;
    private const int MaxErrorSummaryLength = 1000;

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IEmailSender _emailSender;
    private readonly SiteInfoOptions _siteInfo;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(
        IUnitOfWork uow,
        IMapper mapper,
        IEmailSender emailSender,
        IOptionsSnapshot<SiteInfoOptions> siteInfo,
        ILogger<NewsletterService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _emailSender = emailSender;
        _siteInfo = siteInfo.Value;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PendingArticleDto>>> GetPendingArticlesAsync(CancellationToken ct = default)
    {
        var articles = await _uow.Articles.GetPendingNewsletterArticlesAsync(LanguageCodes.Default, ct);

        // Aktif abone sayısı tek seferlik hesaplanır — listede her satır için aynı (snapshot anlık).
        var activeSubscriberCount = await _uow.Subscribers.CountAsync(
            s => s.Status == SubscriberStatus.Active, ct);

        var dtos = articles.Select(a => new PendingArticleDto
        {
            Id = a.Id,
            Title = a.Translations.FirstOrDefault()?.Title ?? "(başlık yok)",
            Slug = a.Translations.FirstOrDefault()?.Slug,
            PublishedAt = a.PublishedAt,
            ActiveSubscriberCount = activeSubscriberCount
        }).ToList();

        return Result.Success<IReadOnlyList<PendingArticleDto>>(dtos);
    }

    public async Task<Result<PagedResult<NewsletterJobListDto>>> GetJobHistoryAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 20;

        var paged = await _uow.NewsletterJobs.GetHistoryPagedAsync(page, pageSize, ct);

        // Faz 7.2b-2-fix: Tek seferlik canlı aktif abone snapshot — tüm satırlara aynı değer.
        // Confirm modalı Pending job için TotalRecipients=0 çelişkisini bu alandan okur.
        var liveActive = await _uow.Subscribers.CountAsync(
            s => s.Status == SubscriberStatus.Active, ct);

        var dtos = paged.Items.Select(j =>
        {
            var dto = MapToListDto(j);
            dto.LiveActiveSubscriberCount = liveActive;
            return dto;
        }).ToList();

        var result = new PagedResult<NewsletterJobListDto>(
            dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<NewsletterJobDetailDto>> GetJobByIdAsync(int id, CancellationToken ct = default)
    {
        var job = await _uow.NewsletterJobs.GetByIdWithArticleAsync(id, ct);
        if (job is null)
        {
            return Result.Failure<NewsletterJobDetailDto>(new Error(
                ErrorCodes.Newsletter.JobNotFound, "Bülten kaydı bulunamadı."));
        }

        return Result.Success(MapToDetailDto(job));
    }

    public async Task<Result<int>> CreateJobAsync(int articleId, CancellationToken ct = default)
    {
        var article = await _uow.Articles.GetByIdAsync(articleId, ct);
        if (article is null)
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Newsletter.ArticleNotFound, "Makale bulunamadı."));
        }

        if (article.Status != ArticleStatus.Published)
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Newsletter.ArticleNotPublished,
                "Sadece yayınlanmış makaleler için bülten oluşturulabilir."));
        }

        if (article.NewsletterSentAt.HasValue)
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Newsletter.AlreadySent,
                "Bu makale için bülten daha önce gönderilmiş."));
        }

        var hasActive = await _uow.NewsletterJobs.HasActiveJobForArticleAsync(articleId, ct);
        if (hasActive)
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Newsletter.ActiveJobExists,
                "Bu makale için bekleyen veya işlemde bir bülten kaydı var."));
        }

        var job = new NewsletterJob
        {
            ArticleId = articleId,
            Status = NewsletterJobStatus.Pending
        };

        await _uow.NewsletterJobs.AddAsync(job, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Newsletter job created (Pending): JobId={JobId}, ArticleId={ArticleId}. " +
            "Gönderim NewsletterDispatcher tarafından yapılacak.",
            job.Id, articleId);

        return Result.Success(job.Id);
    }

    // -------------------- Faz 7.2b-2: ProcessJob (batch motor) --------------------

    public async Task<Result> ProcessJobAsync(int jobId, CancellationToken ct = default)
    {
        NewsletterJob? job = null;

        try
        {
            job = await _uow.NewsletterJobs.GetByIdWithArticleAsync(jobId, ct);
            if (job is null)
            {
                return Result.Failure(new Error(
                    ErrorCodes.Newsletter.JobNotFound, "Bülten kaydı bulunamadı."));
            }

            if (job.Status != NewsletterJobStatus.Pending)
            {
                return Result.Failure(new Error(
                    ErrorCodes.Newsletter.JobNotPending,
                    $"Sadece Pending job işlenebilir. Mevcut durum: {job.Status}."));
            }

            if (job.Article is null)
            {
                return Result.Failure(new Error(
                    ErrorCodes.Newsletter.ArticleNotFound,
                    "Job'a bağlı makale bulunamadı."));
            }

            var subscribers = await _uow.Subscribers.FindAsync(
                s => s.Status == SubscriberStatus.Active, ct);

            // Sending'e geçir + TotalRecipients snapshot — DB'ye yaz (admin UI durumu görsün)
            job.Status = NewsletterJobStatus.Sending;
            job.StartedAt = DateTime.UtcNow;
            job.TotalRecipients = subscribers.Count;
            await _uow.SaveChangesAsync(ct);

            // Aktif abone yoksa: idempotent Completed — makaleyi de gönderildi say (mükerrer engellenir)
            if (subscribers.Count == 0)
            {
                job.Status = NewsletterJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.ErrorSummary = "Aktif abone yok — gönderim yapılmadı.";
                job.Article.NewsletterSentAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
                _logger.LogInformation("Newsletter job completed (0 recipients): JobId={JobId}", jobId);
                return Result.Success();
            }

            await SendToSubscribersAsync(job, subscribers, ct);

            // Final: Completed + Article.NewsletterSentAt set (makale pending listesinden düşer)
            job.Status = NewsletterJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.Article.NewsletterSentAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Newsletter job completed: JobId={JobId}, Sent={Sent}, Failed={Failed}, Total={Total}",
                jobId, job.SentCount, job.FailedCount, job.TotalRecipients);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            // App shutdown veya manuel iptal — job'u Failed işaretle, devam etmeyen iş gibi davran
            _logger.LogWarning("Newsletter job cancelled: JobId={JobId}", jobId);
            await TryMarkFailedAsync(job, "Gönderim iptal edildi (app shutdown veya manuel)");
            return Result.Failure(new Error(ErrorCodes.Common.Unexpected, "İptal edildi."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Newsletter job processing failed: JobId={JobId}", jobId);
            await TryMarkFailedAsync(job, ex.Message);
            return Result.Failure(new Error(ErrorCodes.Common.Unexpected, ex.Message));
        }
    }

    private async Task SendToSubscribersAsync(
        NewsletterJob job, IReadOnlyList<Subscriber> subscribers, CancellationToken ct)
    {
        var article = job.Article!;
        var translation = article.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCodes.Default);
        var title = translation?.Title ?? "(başlık yok)";
        var slug = translation?.Slug ?? string.Empty;
        var excerpt = translation?.Excerpt;
        var baseUrl = (_siteInfo.BaseUrl ?? string.Empty).TrimEnd('/');
        var articleUrl = $"{baseUrl}{PublicRouteSegments.ArticleDetailPath(LanguageCodes.Default, slug)}";
        var featuredImageUrl = ComposeAbsoluteUrl(article.FeaturedImageUrl, baseUrl);
        var subject = $"[{_siteInfo.Name}] {title}";
        var errorSamples = new List<string>();

        for (var i = 0; i < subscribers.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var sub = subscribers[i];

            try
            {
                var unsubUrl = $"{baseUrl}{PublicRouteSegments.UnsubscribePath(LanguageCodes.Default, sub.UnsubscribeToken)}";
                var model = new NewsletterEmailModel(
                    ArticleTitle: title,
                    ArticleExcerpt: excerpt,
                    FeaturedImageUrl: featuredImageUrl,
                    ArticleUrl: articleUrl,
                    UnsubscribeUrl: unsubUrl,
                    SiteName: _siteInfo.Name);

                var html = NewsletterEmailTemplate.Render(model);
                await _emailSender.SendAsync(sub.Email, subject, html, ct);
                job.SentCount++;
            }
            catch (OperationCanceledException)
            {
                throw; // dışta yakalanır
            }
            catch (Exception ex)
            {
                job.FailedCount++;
                _logger.LogWarning(ex,
                    "Newsletter send failed: JobId={JobId}, Email={Email}", job.Id, sub.Email);
                if (errorSamples.Count < MaxErrorSamplesInSummary)
                {
                    errorSamples.Add($"{sub.Email}: {ex.Message}");
                }
            }

            // Her batchSize'da DB ilerleme + rate limit
            if ((i + 1) % BatchSize == 0 && (i + 1) < subscribers.Count)
            {
                await _uow.SaveChangesAsync(ct);
                await Task.Delay(BatchDelayMs, ct);
            }
        }

        if (errorSamples.Count > 0)
        {
            job.ErrorSummary = TruncateSummary(string.Join(" | ", errorSamples));
        }
    }

    private async Task TryMarkFailedAsync(NewsletterJob? job, string errorMessage)
    {
        if (job is null) return;
        try
        {
            job.Status = NewsletterJobStatus.Failed;
            job.ErrorSummary = TruncateSummary(errorMessage);
            job.CompletedAt = DateTime.UtcNow;
            // Article.NewsletterSentAt SET ETME — Failed job tekrar denenebilir kalmalı
            await _uow.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception persistEx)
        {
            _logger.LogError(persistEx,
                "Failed to persist Failed status for newsletter job {JobId}", job.Id);
        }
    }

    private static string TruncateSummary(string message)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        return message.Length > MaxErrorSummaryLength
            ? message.Substring(0, MaxErrorSummaryLength)
            : message;
    }

    private static string? ComposeAbsoluteUrl(string? relativeOrAbsolute, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;
        if (relativeOrAbsolute.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeOrAbsolute.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relativeOrAbsolute;
        return $"{baseUrl}/{relativeOrAbsolute.TrimStart('/')}";
    }

    // -------------------- helpers --------------------

    private static NewsletterJobListDto MapToListDto(NewsletterJob job) => new()
    {
        Id = job.Id,
        ArticleId = job.ArticleId,
        ArticleTitle = job.Article?.Translations.FirstOrDefault()?.Title ?? "(başlık yok)",
        Status = job.Status,
        TotalRecipients = job.TotalRecipients,
        SentCount = job.SentCount,
        FailedCount = job.FailedCount,
        StartedAt = job.StartedAt,
        CompletedAt = job.CompletedAt,
        CreatedAt = job.CreatedAt,
        ErrorSummary = job.ErrorSummary
    };

    private static NewsletterJobDetailDto MapToDetailDto(NewsletterJob job) => new()
    {
        Id = job.Id,
        ArticleId = job.ArticleId,
        ArticleTitle = job.Article?.Translations.FirstOrDefault()?.Title ?? "(başlık yok)",
        Status = job.Status,
        TotalRecipients = job.TotalRecipients,
        SentCount = job.SentCount,
        FailedCount = job.FailedCount,
        StartedAt = job.StartedAt,
        CompletedAt = job.CompletedAt,
        ErrorSummary = job.ErrorSummary,
        CreatedAt = job.CreatedAt
    };
}
