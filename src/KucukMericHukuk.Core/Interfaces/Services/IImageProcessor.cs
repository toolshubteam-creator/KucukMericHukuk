namespace KucukMericHukuk.Core.Interfaces.Services;

public class ProcessedImageResult
{
    public Stream MainImage { get; set; } = Stream.Null;
    public Stream Thumbnail { get; set; } = Stream.Null;
    public int Width { get; set; }
    public int Height { get; set; }
}

public interface IImageProcessor
{
    Task<ProcessedImageResult> ProcessAsync(Stream input, CancellationToken ct = default);
}
