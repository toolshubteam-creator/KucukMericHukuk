namespace KucukMericHukuk.Core.DTOs.Google;

public class GoogleApiClientStatusDto
{
    public bool IsConfigured { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string? CredentialSource { get; set; }
    public string Message { get; set; } = string.Empty;
}
