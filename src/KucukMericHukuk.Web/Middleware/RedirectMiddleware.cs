using System.Text.RegularExpressions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Web.Middleware;

/// <summary>
/// Faz 7.4.2 — URL yönlendirme middleware. İki kaynaktan kontrol:
///   1. <c>Redirects</c> tablosu (admin manuel, `FromPath` tam eşleşme) → `StatusCode` ile 301/302
///   2. <c>SlugHistories</c> tablosu (sistem otomatik — 7.4.3) → path regex ile entity ayıklanır,
///      eski slug history'de bulunursa entity'nin GÜNCEL slug'ı resolve edilir → 301
/// Eşleşme yoksa <c>next()</c> — normal akış (controller route → 404 → NotFoundLogging).
///
/// Pipeline kritik: <c>UseStatusCodePagesWithReExecute</c> ve
/// <c>NotFoundLoggingMiddleware</c> (7.3.2)'den ÖNCE konumlanır. Redirect olan URL
/// 404 üretmez ve `NotFoundLog`'a düşmez (7.3 çakışma sıfır).
///
/// Soft-deleted entity: SlugHistory match olsa bile current slug resolve null döner
/// (`AppDbContext` query filter `!IsDeleted` ile soft-deleted parent'lar otomatik
/// gizlenir) → redirect yapılmaz, 404'e bırakılır.
///
/// Best-effort: lookup veya hit hatası request akışını çökertmez — try/catch ile
/// sessiz log, sonra <c>next()</c>.
/// </summary>
public class RedirectMiddleware
{
    /// <summary>Slug-bazlı sayfalar — path regex (culture + segment + slug).</summary>
    private static readonly Regex SluggedPathRegex = new(
        @"^/(?<culture>[a-z]{2}-[A-Z]{2})/(?<segment>Articles|Services|Attorneys|Pages)/(?<slug>[^/]+?)/?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>NotFoundLogging ile aynı filtre seti — gürültü engelleme.</summary>
    private static readonly string[] IgnoredPathPrefixes =
    {
        "/css", "/js", "/lib", "/images", "/fonts", "/favicon",
        "/admin",
        "/Error"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<RedirectMiddleware> _logger;

    public RedirectMiddleware(
        RequestDelegate next,
        ILogger<RedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (ShouldIgnore(path))
        {
            await _next(context);
            return;
        }

        try
        {
            if (await TryHandleManualRedirectAsync(context, path))
            {
                return;
            }

            if (await TryHandleSlugHistoryRedirectAsync(context, path))
            {
                return;
            }
        }
        catch (Exception ex)
        {
            // Lookup veya redirect yaratırken hata — request'i çökertmeyiz.
            _logger.LogError(ex,
                "RedirectMiddleware lookup failed for {Path}, pass-through to next", path);
        }

        await _next(context);
    }

    private async Task<bool> TryHandleManualRedirectAsync(HttpContext context, string path)
    {
        var uow = context.RequestServices.GetRequiredService<IUnitOfWork>();
        var redirect = await uow.Redirects.GetByFromPathAsync(path, context.RequestAborted);
        if (redirect is null) return false;

        // Runtime self-redirect erken reddi (cycle koruması — tam zincir tespiti 7.4.3
        // insert-time validation'da). FromPath == ToPath ise sonsuz tarayıcı döngüsü olur.
        if (string.Equals(redirect.FromPath, redirect.ToPath, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Redirect self-loop detected, pass-through: FromPath=={ToPath}", redirect.FromPath);
            return false;
        }

        // Best-effort hit kaydı — başarısız olsa bile redirect yine yapılır.
        try
        {
            await uow.Redirects.RecordHitAsync(redirect.Id, context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Redirect hit count update failed (Id={Id}), redirect devam ediyor", redirect.Id);
        }

        IssueRedirect(context, redirect.ToPath, redirect.StatusCode);
        return true;
    }

    private async Task<bool> TryHandleSlugHistoryRedirectAsync(HttpContext context, string path)
    {
        var match = TryParseSluggedPath(path);
        if (match is null) return false;

        var uow = context.RequestServices.GetRequiredService<IUnitOfWork>();
        var history = await uow.SlugHistories.FindCurrentAsync(
            match.Value.EntityType, match.Value.Lang, match.Value.OldSlug,
            context.RequestAborted);
        if (history is null) return false;

        var currentSlug = await ResolveCurrentSlugAsync(
            context, history.EntityType, history.EntityId, history.LanguageCode,
            context.RequestAborted);
        if (currentSlug is null)
        {
            // Entity soft-deleted veya translation yok → redirect etme, 404'e bırak.
            return false;
        }

        if (string.Equals(currentSlug, match.Value.OldSlug, StringComparison.Ordinal))
        {
            // Eski slug = güncel slug — döngü olur, atla.
            return false;
        }

        var newPath = $"{match.Value.PathPrefix}/{currentSlug}";
        // Slug history kaynaklı yönlendirme her zaman 301 (kalıcı SEO transfer).
        IssueRedirect(context, newPath, 301);
        return true;
    }

    private static (SluggedEntityType EntityType, string Lang, string OldSlug, string PathPrefix)?
        TryParseSluggedPath(string path)
    {
        var m = SluggedPathRegex.Match(path);
        if (!m.Success) return null;

        var lang = m.Groups["culture"].Value;
        var segment = m.Groups["segment"].Value;
        var slug = m.Groups["slug"].Value;

        SluggedEntityType? type = segment switch
        {
            "Articles" => SluggedEntityType.Article,
            "Services" => SluggedEntityType.Service,
            "Attorneys" => SluggedEntityType.Attorney,
            "Pages" => SluggedEntityType.Page,
            _ => null
        };
        if (type is null) return null;

        var pathPrefix = $"/{lang}/{segment}";
        return (type.Value, lang, slug, pathPrefix);
    }

    /// <summary>
    /// EntityId + lang → güncel translation slug. AppDbContext'in global query filter'ı
    /// soft-deleted parent entity'leri otomatik gizler (her translation tablosu
    /// `!t.Parent.IsDeleted` filter taşıyor) → null dönerse entity erişilemez,
    /// redirect atlanır.
    /// </summary>
    private static async Task<string?> ResolveCurrentSlugAsync(
        HttpContext context,
        SluggedEntityType entityType,
        int entityId,
        string lang,
        CancellationToken ct)
    {
        var db = context.RequestServices.GetRequiredService<AppDbContext>();

        return entityType switch
        {
            SluggedEntityType.Article =>
                await db.Set<ArticleTranslation>()
                    .Where(t => t.ArticleId == entityId && t.LanguageCode == lang)
                    .Select(t => t.Slug)
                    .FirstOrDefaultAsync(ct),

            SluggedEntityType.Page =>
                await db.Set<PageTranslation>()
                    .Where(t => t.PageId == entityId && t.LanguageCode == lang)
                    .Select(t => t.Slug)
                    .FirstOrDefaultAsync(ct),

            SluggedEntityType.Service =>
                await db.Set<ServiceTranslation>()
                    .Where(t => t.ServiceId == entityId && t.LanguageCode == lang)
                    .Select(t => t.Slug)
                    .FirstOrDefaultAsync(ct),

            SluggedEntityType.Attorney =>
                await db.Set<AttorneyTranslation>()
                    .Where(t => t.AttorneyId == entityId && t.LanguageCode == lang)
                    .Select(t => t.Slug)
                    .FirstOrDefaultAsync(ct),

            // Category/Tag direkt URL hedefi değil (filtre olarak listede görünür) — redirect yok.
            _ => null
        };
    }

    private static void IssueRedirect(HttpContext context, string toPath, int statusCode)
    {
        // 301 (kalıcı) veya 302 (geçici). Query string ToPath içinde olabilir; admin
        // ToPath'i nihai hedef olarak girer — request query'si auto-append YOK (UX kararı).
        context.Response.StatusCode = statusCode;
        context.Response.Headers.Location = toPath;
    }

    private static bool ShouldIgnore(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        foreach (var prefix in IgnoredPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }

        if (path.IndexOf("/Error/", StringComparison.OrdinalIgnoreCase) >= 0) return true;

        return false;
    }
}
