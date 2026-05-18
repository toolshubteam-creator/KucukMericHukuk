using KucukMericHukuk.Core.Interfaces;
using Microsoft.AspNetCore.Diagnostics;

namespace KucukMericHukuk.Web.Middleware;

/// <summary>
/// Faz 7.3.2 — 404 status'ünde son bulan istekleri `NotFoundLogs` aggregate'ine işler.
/// Best-effort log: kullanıcı 404 sayfasını gördükten sonra arka planda kaydedilir,
/// log hatası 404 deneyimini bozmaz.
///
/// Pipeline order kritik: middleware `UseStatusCodePagesWithReExecute`'ten ÖNCE
/// kurulur. `await _next()` döndüğünde:
///   • Response.StatusCode = final (re-execute sonrası, controller-set 404)
///   • IStatusCodeReExecuteFeature = StatusCodePages middleware tarafından set,
///     `OriginalPath`/`OriginalQueryString` orijinal kırık URL'i taşır
/// Bu yüzden `/tr-TR/eski-sayfa` kaydedilir, re-execute hedefi `/tr-TR/Error/404` DEĞİL.
/// </summary>
public class NotFoundLoggingMiddleware
{
    /// <summary>NotFoundLog.Url NVARCHAR(850) — bu uzunluğu aşan URL'ler kesilir.</summary>
    private const int MaxUrlLength = 850;

    /// <summary>
    /// 404 yakalanmayacak path prefix'leri. Asset/admin/error sayfası 404'lerini gürültü kabul
    /// ederiz — admin 404 son-kullanıcıya görünmez, Error/* re-execute hedefidir.
    /// </summary>
    private static readonly string[] IgnoredPathPrefixes =
    {
        "/css", "/js", "/lib", "/images", "/fonts", "/favicon",
        "/admin",
        "/Error"
    };

    /// <summary>Statik asset uzantıları — 404 olsalar bile log'lanmaz.</summary>
    private static readonly string[] IgnoredPathExtensions =
    {
        ".css", ".js", ".map",
        ".png", ".jpg", ".jpeg", ".webp", ".svg", ".gif",
        ".ico", ".woff", ".woff2", ".ttf", ".eot",
        ".pdf", ".zip"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<NotFoundLoggingMiddleware> _logger;

    public NotFoundLoggingMiddleware(
        RequestDelegate next,
        ILogger<NotFoundLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode != StatusCodes.Status404NotFound) return;

        // POST/PUT/DELETE 404'leri tipik olarak API/form gürültüsü (route mismatch sırasında
        // farklı parametre denemeleri) — sadece GET kullanıcı navigasyonunu yansıtır.
        if (!HttpMethods.IsGet(context.Request.Method)) return;

        var (path, queryString) = ResolveOriginalUrl(context);

        if (ShouldIgnore(path)) return;

        var url = path + queryString;

        if (url.Length > MaxUrlLength)
        {
            _logger.LogDebug(
                "notfound url truncated {OriginalLength} -> {MaxLength}: {UrlPreview}",
                url.Length, MaxUrlLength,
                url[..Math.Min(120, url.Length)]);
            url = url[..MaxUrlLength];
        }

        try
        {
            var uow = context.RequestServices.GetRequiredService<IUnitOfWork>();
            await uow.NotFoundLogs.RecordHitAsync(
                url,
                StringOrNull(context.Request.Headers.Referer.ToString()),
                StringOrNull(context.Request.Headers.UserAgent.ToString()),
                context.Connection.RemoteIpAddress?.ToString(),
                context.RequestAborted);
        }
        catch (Exception ex)
        {
            // 404 sayfası kullanıcıya zaten render edildi — log fail'ı sessiz geçer.
            _logger.LogError(ex,
                "NotFoundLoggingMiddleware: RecordHitAsync hatasi - log atlandi (Url={Url})", url);
        }
    }

    /// <summary>
    /// Orijinal URL: re-execute öncesi path+query. UseStatusCodePagesWithReExecute
    /// feature'ı set'ler; yoksa (re-execute olmadan dönen 404) Request kendisi.
    /// </summary>
    private static (string Path, string QueryString) ResolveOriginalUrl(HttpContext context)
    {
        var feat = context.Features.Get<IStatusCodeReExecuteFeature>();
        if (feat is not null)
        {
            return (feat.OriginalPath ?? string.Empty, feat.OriginalQueryString ?? string.Empty);
        }
        return (
            context.Request.Path.Value ?? string.Empty,
            context.Request.QueryString.Value ?? string.Empty);
    }

    private static bool ShouldIgnore(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        foreach (var prefix in IgnoredPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }

        // Culture-prefixed error: /tr-TR/Error/404 vb. (IgnoredPathPrefixes "/Error" sadece kök'ü tutar)
        if (path.IndexOf("/Error/", StringComparison.OrdinalIgnoreCase) >= 0) return true;

        foreach (var ext in IgnoredPathExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static string? StringOrNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
