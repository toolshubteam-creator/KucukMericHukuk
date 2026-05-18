namespace KucukMericHukuk.Core.DTOs.Redirect;

/// <summary>Admin liste sorgusu (Faz 7.4.3).</summary>
public class RedirectQueryDto
{
    /// <summary>FromPath/ToPath/OldSlug substring araması.</summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// null = hepsi, true = sadece aktif, false = sadece pasif. SlugHistory
    /// satırları her zaman aktif sayılır (bilgi amaçlı); `false` filtre
    /// uygulanırsa SlugHistory satırları liste dışında kalır.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// null = hepsi (Manuel + SlugHistory), Manual = sadece manuel redirect,
    /// SlugHistory = sadece otomatik slug izi (Faz 7.4.3a-ek).
    /// </summary>
    public RedirectSource? Source { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
