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

    /// <summary>
    /// Production environment'ta SiteKey + SecretKey zorunludur. Eksikse startup fail.
    /// Demo key'lere (1x00000…) düşmek YASAK — production'da gerçek Cloudflare key'leri kullanılır.
    /// Çağrı: Program.cs'te <c>app.Environment.IsProduction()</c> kontrolünden sonra (Faz 6.25).
    /// </summary>
    public void ValidateForProduction()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(SiteKey)) missing.Add("Turnstile__SiteKey");
        if (string.IsNullOrWhiteSpace(SecretKey)) missing.Add("Turnstile__SecretKey");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Production environment'ta şu environment variable'lar zorunludur: " +
                string.Join(", ", missing) +
                ". appsettings.Production.json secret içermez; bu değerler env-var ile set edilir.");
        }
    }
}
