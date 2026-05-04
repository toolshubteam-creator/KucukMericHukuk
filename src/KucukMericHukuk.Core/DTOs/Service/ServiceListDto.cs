namespace KucukMericHukuk.Core.DTOs.Service;

public class ServiceListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Icon { get; set; }
    public string? FeaturedImage { get; set; }
    public int DisplayOrder { get; set; }
}
