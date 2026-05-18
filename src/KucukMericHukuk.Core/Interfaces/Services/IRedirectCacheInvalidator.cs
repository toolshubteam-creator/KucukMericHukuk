namespace KucukMericHukuk.Core.Interfaces.Services;

/// <summary>
/// Manuel redirect cache invalidasyon (Faz 7.4.3a, 7.4.2 cache iskeletini wire eder).
/// Web katmanında IMemoryCache impl (`MemoryRedirectCacheInvalidator`) — Business
/// katmanı (RedirectService) bu soyutlamaya bağlıdır, IMemoryCache'i bilmez.
/// </summary>
public interface IRedirectCacheInvalidator
{
    /// <summary>Belirli FromPath için cache anahtarını temizler.</summary>
    void Invalidate(string fromPath);
}
