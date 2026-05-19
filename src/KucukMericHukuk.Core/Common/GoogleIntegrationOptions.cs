namespace KucukMericHukuk.Core.Common;

public class GoogleIntegrationOptions
{
    public const string SectionName = "GoogleIntegration";

    public string ApplicationName { get; set; } = "KucukMericHukuk CMS";

    /// <summary>Service account JSON. Production'da environment variable ile set edilir.</summary>
    public string? ServiceAccountJson { get; set; }

    /// <summary>Service account JSON'in base64 hali. Container/env-var icin satir sonu problemi yasatmaz.</summary>
    public string? ServiceAccountJsonBase64 { get; set; }

    /// <summary>Server uzerindeki service account JSON dosya yolu. Repo icine koyulmaz.</summary>
    public string? ServiceAccountFilePath { get; set; }

    public bool HasCredentialSource =>
        !string.IsNullOrWhiteSpace(ServiceAccountJson) ||
        !string.IsNullOrWhiteSpace(ServiceAccountJsonBase64) ||
        !string.IsNullOrWhiteSpace(ServiceAccountFilePath);
}
