using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;

namespace KucukMericHukuk.Web.Middleware;

public class SecurityHeadersMiddleware
{
    /// <summary>
    /// Faz 6.20: per-request CSP nonce'a Razor view'lardan + helper'lardan
    /// (JsonLdHelpers) erişim için HttpContext.Items anahtarı.
    /// </summary>
    public const string NonceItemKey = "csp-nonce";

    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _isDevelopment = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Faz 6.20: per-request kriptografik nonce. View render'dan ve OnStarting
        // callback'inden ÖNCE Items'a yazılır — JSON-LD helper'ı + inline script'ler
        // bu nonce'u okuyabilsin diye sıra kritik.
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceItemKey] = nonce;

        var contentSecurityPolicy = BuildCsp(nonce, _isDevelopment);

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Content-Security-Policy"] = contentSecurityPolicy;
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string BuildCsp(string nonce, bool isDevelopment)
    {
        // Faz 6.17: tüm vendor JS/CSS self-host (wwwroot/lib/), CDN allowlist tasfiye.
        // Geriye sadece Cloudflare Turnstile kaldı (Cloudflare-managed, self-host edilmez).
        // connect-src: fetch/XHR/WebSocket destinations
        // - Production: 'self' + Turnstile (challenges.cloudflare.com)
        // - Development: ek olarak ws://localhost:* (BrowserLink + browser-refresh) + http(s)://localhost:*
        var connectSrc = "'self' https://challenges.cloudflare.com";
        if (isDevelopment)
        {
            connectSrc += " ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*";
        }

        return
            "default-src 'self'; " +
            // Faz 6.20: 'unsafe-inline' kaldırıldı — inline script'ler (JSON-LD) per-request
            // nonce ile yetkilendirilir. Turnstile dış src script olduğu için host allowlist yeterli.
            $"script-src 'self' 'nonce-{nonce}' https://challenges.cloudflare.com; " +
            // style-src 'unsafe-inline' bilinçli korundu — Faz 6.20 kapsam dışı (ayrı DEFERRED maddesi).
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self' data:; " +
            "img-src 'self' data: https:; " +
            $"connect-src {connectSrc}; " +
            "frame-src https://challenges.cloudflare.com; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';";
    }
}
