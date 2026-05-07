using FluentAssertions;
using KucukMericHukuk.Infrastructure.FileStorage;
using SkiaSharp;

namespace KucukMericHukuk.Tests.Infrastructure;

public class SkiaSharpProcessorTests
{
    private readonly SkiaSharpProcessor _sut = new();

    private static MemoryStream CreatePngStream(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Red);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ProcessAsync_LargeImage_ResizesToMaxLongEdge()
    {
        await using var input = CreatePngStream(4000, 3000);

        var result = await _sut.ProcessAsync(input);

        result.Width.Should().Be(2000);
        result.Height.Should().Be(1500);
        result.MainImage.Length.Should().BeGreaterThan(0);
        result.Thumbnail.Length.Should().BeGreaterThan(0);

        await result.MainImage.DisposeAsync();
        await result.Thumbnail.DisposeAsync();
    }

    [Fact]
    public async Task ProcessAsync_SmallImage_KeepsOriginalDimensions()
    {
        await using var input = CreatePngStream(800, 600);

        var result = await _sut.ProcessAsync(input);

        result.Width.Should().Be(800);
        result.Height.Should().Be(600);

        await result.MainImage.DisposeAsync();
        await result.Thumbnail.DisposeAsync();
    }

    [Fact]
    public async Task ProcessAsync_AlwaysProducesThumbnail_LongEdge300()
    {
        await using var input = CreatePngStream(1200, 900);

        var result = await _sut.ProcessAsync(input);

        result.Thumbnail.Position = 0;
        using var thumbBitmap = SKBitmap.Decode(result.Thumbnail);
        thumbBitmap.Width.Should().Be(300);
        thumbBitmap.Height.Should().Be(225);

        await result.MainImage.DisposeAsync();
        await result.Thumbnail.DisposeAsync();
    }

    [Fact]
    public async Task ProcessAsync_PortraitImage_ScalesByHeight()
    {
        await using var input = CreatePngStream(1500, 3000);

        var result = await _sut.ProcessAsync(input);

        result.Height.Should().Be(2000);
        result.Width.Should().Be(1000);

        await result.MainImage.DisposeAsync();
        await result.Thumbnail.DisposeAsync();
    }
}
