namespace KucukMericHukuk.Core.Common;

public class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>true ise Turnstile doğrulama aktif; false ise verify her zaman pass.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Public key, HTML widget'ında görünür. Demo: 1x00000000000000000000AA (always pass).</summary>
    public string SiteKey { get; set; } = "1x00000000000000000000AA";

    /// <summary>Server-side secret. Demo: 1x0000000000000000000000000000000AA (always pass).</summary>
    public string SecretKey { get; set; } = "1x0000000000000000000000000000000AA";

    /// <summary>HTTP request timeout (ms) — Cloudflare verify endpoint için.</summary>
    public int VerifyTimeoutMs { get; set; } = 5000;
}
