namespace KucukMericHukuk.Core.Interfaces.Services;

public interface ITurnstileVerifier
{
    /// <summary>
    /// Turnstile token'ını Cloudflare'e doğrulatır.
    /// Options.Enabled=false ise her zaman true döner.
    /// HTTP timeout/exception → false döner (fail-secure).
    /// </summary>
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default);
}
