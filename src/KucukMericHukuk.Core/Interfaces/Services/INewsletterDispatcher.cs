namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// Faz 7.2b-2: NewsletterService.ProcessJobAsync'i arka planda çalıştıran fire-and-forget sarmalayıcı.
/// Task.Run + IServiceScopeFactory pattern (IHostedService değil — tek-seferlik admin tetiği).
/// Exception arka planda yutulur ama ProcessJobAsync içi job.Status=Failed + ErrorSummary'ye yazar.
/// </summary>
public interface INewsletterDispatcher
{
    /// <summary>
    /// Job'u arka plana atar (await BEKLEMEZ). Controller hemen geri döner, kullanıcı UI'da
    /// durum geçişini "Gönderim Geçmişi" tablosunda yenileyerek izler.
    /// </summary>
    void Dispatch(int jobId);
}
