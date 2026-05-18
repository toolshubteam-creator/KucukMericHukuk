namespace KucukMericHukuk.Core.DTOs.Redirect;

/// <summary>Admin liste sorgusu (Faz 7.4.3).</summary>
public class RedirectQueryDto
{
    /// <summary>FromPath veya ToPath substring araması.</summary>
    public string? Keyword { get; set; }

    /// <summary>null = hepsi, true = sadece aktif, false = sadece pasif.</summary>
    public bool? IsActive { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
