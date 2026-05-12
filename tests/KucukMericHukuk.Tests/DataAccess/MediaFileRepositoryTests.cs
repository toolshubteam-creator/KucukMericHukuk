using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Repositories;
using KucukMericHukuk.Tests.Infrastructure;

namespace KucukMericHukuk.Tests.DataAccess;

public class MediaFileRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public MediaFileRepositoryTests()
    {
        _factory = new TestDbContextFactory();
    }

    private static MediaFile Build(
        string name,
        bool isPublic,
        string contentType = "image/webp",
        DateTime? createdAt = null,
        string? sha = null)
    {
        return new MediaFile
        {
            FileName = name + ".webp",
            OriginalFileName = name + ".jpg",
            RelativePath = $"uploads/2026/05/{name}.webp",
            ThumbnailRelativePath = $"uploads/2026/05/{name}_thumb.webp",
            ContentType = contentType,
            FileSizeBytes = 1024,
            Width = 800,
            Height = 600,
            Sha256 = sha ?? name + new string('0', 64 - name.Length),
            AltText = "alt-" + name,
            IsPublic = isPublic,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };
    }

    [Fact]
    public async Task GetPublicPagedAsync_OnlyPublicImages_AreIncluded()
    {
        await using var context = _factory.CreateContext();
        var repo = new MediaFileRepository(context);

        await repo.AddAsync(Build("public-1", isPublic: true));
        await repo.AddAsync(Build("public-2", isPublic: true));
        await repo.AddAsync(Build("private-3", isPublic: false));
        await context.SaveChangesAsync();

        var paged = await repo.GetPublicPagedAsync(page: 1, pageSize: 24);

        paged.TotalCount.Should().Be(2);
        paged.Items.Should().OnlyContain(m => m.IsPublic);
    }

    [Fact]
    public async Task GetPublicPagedAsync_ExcludesNonImageContentType()
    {
        await using var context = _factory.CreateContext();
        var repo = new MediaFileRepository(context);

        await repo.AddAsync(Build("img-1", isPublic: true, contentType: "image/webp"));
        await repo.AddAsync(Build("pdf-1", isPublic: true, contentType: "application/pdf"));
        await repo.AddAsync(Build("video-1", isPublic: true, contentType: "video/mp4"));
        await context.SaveChangesAsync();

        var paged = await repo.GetPublicPagedAsync(page: 1, pageSize: 24);

        paged.TotalCount.Should().Be(1);
        paged.Items.Should().OnlyContain(m => m.ContentType.StartsWith("image/"));
    }

    [Fact]
    public async Task GetPublicPagedAsync_OrderedByCreatedAtDescending()
    {
        await using var context = _factory.CreateContext();
        var repo = new MediaFileRepository(context);

        var older = Build("older", isPublic: true, createdAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = Build("newer", isPublic: true, createdAt: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));

        await repo.AddAsync(older);
        await repo.AddAsync(newer);
        await context.SaveChangesAsync();

        var paged = await repo.GetPublicPagedAsync(page: 1, pageSize: 24);

        paged.Items.Should().HaveCount(2);
        paged.Items[0].FileName.Should().Be("newer.webp");
        paged.Items[1].FileName.Should().Be("older.webp");
    }

    [Fact]
    public async Task GetPublicPagedAsync_SoftDeleted_Excluded()
    {
        await using var context = _factory.CreateContext();
        var repo = new MediaFileRepository(context);

        var visible = Build("visible", isPublic: true);
        var deleted = Build("deleted", isPublic: true);
        await repo.AddAsync(visible);
        await repo.AddAsync(deleted);
        await context.SaveChangesAsync();

        repo.Delete(deleted);
        await context.SaveChangesAsync();

        var paged = await repo.GetPublicPagedAsync(page: 1, pageSize: 24);

        paged.TotalCount.Should().Be(1);
        paged.Items[0].FileName.Should().Be("visible.webp");
    }

    public void Dispose() => _factory.Dispose();
}
