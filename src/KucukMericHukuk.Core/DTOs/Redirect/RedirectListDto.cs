namespace KucukMericHukuk.Core.DTOs.Redirect;

/// <summary>Admin "Yönlendirmeler" liste sayfası (Faz 7.4.3).</summary>
public class RedirectListDto
{
    public int Id { get; set; }
    public string FromPath { get; set; } = string.Empty;
    public string ToPath { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool IsActive { get; set; }
    public int HitCount { get; set; }
    public DateTime? LastHitAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
