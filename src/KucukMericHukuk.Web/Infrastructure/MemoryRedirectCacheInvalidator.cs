using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Middleware;
using Microsoft.Extensions.Caching.Memory;

namespace KucukMericHukuk.Web.Infrastructure;

/// <summary>
/// Faz 7.4.3a — `IRedirectCacheInvalidator` IMemoryCache impl. Business katmanı
/// Web bağımlılığı taşımaz; bu adapter Web tarafında impl edilir + DI'ye kayıt.
/// </summary>
public class MemoryRedirectCacheInvalidator : IRedirectCacheInvalidator
{
    private readonly IMemoryCache _cache;

    public MemoryRedirectCacheInvalidator(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Invalidate(string fromPath)
    {
        if (string.IsNullOrEmpty(fromPath)) return;
        _cache.Remove(RedirectMiddleware.CacheKeyPrefix + fromPath);
    }
}
