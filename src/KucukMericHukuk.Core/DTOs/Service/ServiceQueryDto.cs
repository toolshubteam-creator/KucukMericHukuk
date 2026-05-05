namespace KucukMericHukuk.Core.DTOs.Service;

public class ServiceQueryDto
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; } = false;
    public string LanguageCode { get; set; } = "tr-TR";
}
