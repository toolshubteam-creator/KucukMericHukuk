namespace KucukMericHukuk.Core.DTOs.Redirect;

/// <summary>Admin Create/Edit form input (Faz 7.4.3).</summary>
public class RedirectFormDto
{
    public int? Id { get; set; }
    public string FromPath { get; set; } = string.Empty;
    public string ToPath { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 301;
    public bool IsActive { get; set; } = true;
}
