using KucukMericHukuk.Core.Interfaces.Services;

namespace KucukMericHukuk.Web.Infrastructure;

/// <summary>
/// Faz 7.2b-2: Bülten gönderim job'larını arka planda çalıştıran fire-and-forget sarmalayıcı.
/// Task.Run + IServiceScopeFactory pattern (IHostedService değil — tek-seferlik admin tetiği).
///
/// Akış: controller.Dispatch(jobId) → Task.Run → yeni scope → INewsletterService.ProcessJobAsync.
/// Exception arka planda yutulur (log) ama ProcessJobAsync içi job.Status=Failed + ErrorSummary'ye
/// yazdığı için kullanıcı admin UI'da durumu görür. App shutdown'da CancellationToken iletilir.
///
/// DİKKAT — Singleton: scoped servisleri (NewsletterService, DbContext) HER Dispatch çağrısı için
/// yeni scope yaratıp resolve eder. DbContext lifetime sorunu olmaz.
/// </summary>
public class NewsletterDispatcher : INewsletterDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<NewsletterDispatcher> _logger;

    public NewsletterDispatcher(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime appLifetime,
        ILogger<NewsletterDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public void Dispatch(int jobId)
    {
        // App shutdown'da iptal — pending email kalsa bile veritabanı durumu Failed olur.
        var ct = _appLifetime.ApplicationStopping;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INewsletterService>();
                await service.ProcessJobAsync(jobId, ct);
            }
            catch (Exception ex)
            {
                // ProcessJobAsync zaten kendi try/catch'inde Failed işaretliyor — bu güvenlik ağı.
                _logger.LogError(ex,
                    "NewsletterDispatcher beklenmedik exception (ProcessJobAsync üstü): JobId={JobId}", jobId);
            }
        }, ct);

        _logger.LogInformation("Newsletter job dispatched: JobId={JobId}", jobId);
    }
}
