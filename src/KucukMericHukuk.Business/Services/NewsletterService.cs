using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Newsletter;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Business.Services;

public class NewsletterService : INewsletterService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(IUnitOfWork uow, IMapper mapper, ILogger<NewsletterService> logger)
    {
        _uow = uow;
        _mapper = mapper;
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

        var dtos = paged.Items.Select(MapToListDto).ToList();

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
            "Gönderim 7.2b-2 batch processor tarafından yapılacak.",
            job.Id, articleId);

        return Result.Success(job.Id);
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
        CreatedAt = job.CreatedAt
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
