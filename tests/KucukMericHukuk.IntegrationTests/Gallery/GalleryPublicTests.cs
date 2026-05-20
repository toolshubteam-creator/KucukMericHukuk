using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Gallery;

public class GalleryPublicTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public GalleryPublicTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedAsync(params (string Name, bool IsPublic, string ContentType)[] data)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<MediaFile>().RemoveRange(db.Set<MediaFile>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();

        var i = 0;
        foreach (var (name, isPublic, contentType) in data)
        {
            i++;
            db.Set<MediaFile>().Add(new MediaFile
            {
                FileName = name + ".webp",
                OriginalFileName = name + ".jpg",
                RelativePath = $"uploads/2026/05/{name}.webp",
                ThumbnailRelativePath = $"uploads/2026/05/{name}_thumb.webp",
                ContentType = contentType,
                FileSizeBytes = 1024,
                Width = 800,
                Height = 600,
                Sha256 = $"hash{i:D60}",
                AltText = $"alt-{name}",
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetGaleri_ReturnsGrid_WhenPublicImagesExist()
    {
        await SeedAsync(
            ("public-1", true, "image/webp"),
            ("public-2", true, "image/webp"),
            ("private-1", false, "image/webp"));

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/galeri");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Grid markup + public görseller (ASCII-safe)
        html.Should().Contain("gallery-grid");
        html.Should().Contain("public-1_thumb.webp");
        html.Should().Contain("public-2_thumb.webp");
        // Private görsel render edilmez
        html.Should().NotContain("private-1_thumb.webp");
    }

    [Fact]
    public async Task GetGaleri_EmptyDb_ShowsEmptyState()
    {
        await SeedAsync(); // hiç veri yok

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/galeri");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Empty state: grid yok, lightbox yok
        html.Should().NotContain("gallery-grid");
        html.Should().NotContain("gallery-lightbox");
    }

    [Fact]
    public async Task GetGaleri_NonImageContentType_NotShown()
    {
        await SeedAsync(
            ("image-pub", true, "image/webp"),
            ("pdf-pub", true, "application/pdf"));

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/galeri");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("image-pub_thumb.webp");
        html.Should().NotContain("pdf-pub");
    }
}
