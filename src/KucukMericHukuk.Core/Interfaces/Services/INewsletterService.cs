using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Newsletter;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface INewsletterService
{
    /// <summary>Bülten bekleyen makaleler — Published + NewsletterSentAt NULL.</summary>
    Task<Result<IReadOnlyList<PendingArticleDto>>> GetPendingArticlesAsync(CancellationToken ct = default);

    Task<Result<PagedResult<NewsletterJobListDto>>> GetJobHistoryAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<Result<NewsletterJobDetailDto>> GetJobByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Faz 7.2b-1: Pending NewsletterJob oluşturur — GÖNDERMEZ.
    /// Asıl gönderim 7.2b-2 (batch processor) Pending job'ları işleyecek.
    /// Aynı makale için zaten Pending/Sending job varsa Conflict döner.
    /// </summary>
    Task<Result<int>> CreateJobAsync(int articleId, CancellationToken ct = default);

    /// <summary>
    /// Faz 7.2b-2: Pending job'u işler — Sending'e geçer, aktif abonelere batch gönderir,
    /// her ~10 mailde DB'ye ilerleme yazar (SMTP rate limit + Task.Delay), abone-bazlı try/catch.
    /// Başarı: Status=Completed + Article.NewsletterSentAt=now (makale pending listesinden düşer).
    /// Beklenmedik exception: Status=Failed + ErrorSummary, Article.NewsletterSentAt SET EDİLMEZ
    /// (tekrar denenebilir kalır). NewsletterDispatcher arka planda fire-and-forget çağırır.
    /// </summary>
    Task<Result> ProcessJobAsync(int jobId, CancellationToken ct = default);
}
