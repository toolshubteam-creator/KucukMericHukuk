namespace KucukMericHukuk.Core.DTOs.Media;

public class MediaUpdateInputDto
{
    public int Id { get; set; }
    public string? AltText { get; set; }

    /// <summary>Public /Galeri sayfasında listelensin mi?</summary>
    public bool IsPublic { get; set; }
}
