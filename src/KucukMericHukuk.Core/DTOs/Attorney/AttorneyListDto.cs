namespace KucukMericHukuk.Core.DTOs.Attorney;

public class AttorneyListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? ShortBio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int DisplayOrder { get; set; }
}
