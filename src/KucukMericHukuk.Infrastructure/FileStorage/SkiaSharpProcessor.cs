using KucukMericHukuk.Core.Interfaces.Services;
using SkiaSharp;

namespace KucukMericHukuk.Infrastructure.FileStorage;

public class SkiaSharpProcessor : IImageProcessor
{
    private const int MaxLongEdge = 2000;
    private const int ThumbnailLongEdge = 300;
    private const int WebpQuality = 80;

    // SkiaSharp 3.x: SKFilterQuality.High deprecated. Mitchell-Netravali cubic
    // (B=1/3, C=1/3) yüksek kaliteli downscale için tercih edilen sampling.
    private static readonly SKSamplingOptions SamplingOptions =
        new(new SKCubicResampler(1f / 3f, 1f / 3f));

    public Task<ProcessedImageResult> ProcessAsync(Stream input, CancellationToken ct = default)
    {
        input.Position = 0;

        using var original = SKBitmap.Decode(input);
        if (original is null)
            throw new InvalidOperationException("Görsel decode edilemedi (desteklenmeyen veya bozuk format).");

        var (mainW, mainH) = ScaleToFit(original.Width, original.Height, MaxLongEdge);
        SKBitmap mainBitmap;
        if (mainW == original.Width && mainH == original.Height)
        {
            mainBitmap = original.Copy();
        }
        else
        {
            mainBitmap = original.Resize(new SKImageInfo(mainW, mainH), SamplingOptions);
            if (mainBitmap is null)
                throw new InvalidOperationException("Görsel yeniden boyutlandırılamadı (ana görsel).");
        }

        var (thumbW, thumbH) = ScaleToFit(original.Width, original.Height, ThumbnailLongEdge);
        var thumbBitmap = original.Resize(new SKImageInfo(thumbW, thumbH), SamplingOptions);
        if (thumbBitmap is null)
        {
            mainBitmap.Dispose();
            throw new InvalidOperationException("Görsel yeniden boyutlandırılamadı (thumbnail).");
        }

        try
        {
            var mainStream = new MemoryStream();
            using (var skMain = SKImage.FromBitmap(mainBitmap))
            using (var data = skMain.Encode(SKEncodedImageFormat.Webp, WebpQuality))
            {
                if (data is null)
                    throw new InvalidOperationException("WebP encode başarısız (ana görsel).");
                data.SaveTo(mainStream);
            }
            mainStream.Position = 0;

            var thumbStream = new MemoryStream();
            using (var skThumb = SKImage.FromBitmap(thumbBitmap))
            using (var data = skThumb.Encode(SKEncodedImageFormat.Webp, WebpQuality))
            {
                if (data is null)
                    throw new InvalidOperationException("WebP encode başarısız (thumbnail).");
                data.SaveTo(thumbStream);
            }
            thumbStream.Position = 0;

            return Task.FromResult(new ProcessedImageResult
            {
                MainImage = mainStream,
                Thumbnail = thumbStream,
                Width = mainW,
                Height = mainH
            });
        }
        finally
        {
            mainBitmap.Dispose();
            thumbBitmap.Dispose();
        }
    }

    private static (int Width, int Height) ScaleToFit(int width, int height, int maxLongEdge)
    {
        if (Math.Max(width, height) <= maxLongEdge)
            return (width, height);

        if (width >= height)
        {
            var newWidth = maxLongEdge;
            var newHeight = (int)Math.Round(height * (maxLongEdge / (double)width));
            return (newWidth, Math.Max(1, newHeight));
        }
        else
        {
            var newHeight = maxLongEdge;
            var newWidth = (int)Math.Round(width * (maxLongEdge / (double)height));
            return (Math.Max(1, newWidth), newHeight);
        }
    }
}
