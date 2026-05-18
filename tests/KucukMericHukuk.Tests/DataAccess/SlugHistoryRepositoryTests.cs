using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Repositories;
using KucukMericHukuk.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.DataAccess;

/// <summary>
/// Faz 7.4.1 — SlugHistoryRepository: FindCurrent en yeni satır seçimi,
/// slug X→Y→X senaryosu (composite non-unique index).
/// </summary>
public class SlugHistoryRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public SlugHistoryRepositoryTests()
    {
        _factory = new TestDbContextFactory();
    }

    [Fact]
    public async Task FindCurrentAsync_ExistingOldSlug_ReturnsRecord()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.SlugHistories.Add(new SlugHistory
            {
                EntityType = SluggedEntityType.Article,
                EntityId = 42,
                LanguageCode = "tr-TR",
                OldSlug = "eski-makale",
                CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new SlugHistoryRepository(context);

        var result = await repo.FindCurrentAsync(SluggedEntityType.Article, "tr-TR", "eski-makale");

        result.Should().NotBeNull();
        result!.EntityId.Should().Be(42);
    }

    [Fact]
    public async Task FindCurrentAsync_MissingSlug_ReturnsNull()
    {
        await using var context = _factory.CreateContext();
        var repo = new SlugHistoryRepository(context);

        var result = await repo.FindCurrentAsync(SluggedEntityType.Article, "tr-TR", "yok");

        result.Should().BeNull();
    }

    /// <summary>
    /// Slug X → Y → X senaryosu. Aynı (EntityType, Lang, OldSlug=X) için iki satır
    /// oluşur (önce slug=X iken Y'ye geçildi → row1; sonra slug=Y'den X'e geri döndü → row2 OldSlug=Y;
    /// tekrar X'ten farklı bir Z'ye geçildi → row3 OldSlug=X). En yeni X row'u dönmeli.
    /// </summary>
    [Fact]
    public async Task FindCurrentAsync_DuplicateOldSlug_ReturnsMostRecentByCreatedAt()
    {
        var t0 = DateTime.UtcNow.AddDays(-3);
        var t1 = DateTime.UtcNow.AddDays(-1);

        await using (var seed = _factory.CreateContext())
        {
            // İlk: slug=eski → bir-baska oldu (X→Y)
            seed.SlugHistories.Add(new SlugHistory
            {
                EntityType = SluggedEntityType.Article,
                EntityId = 100,
                LanguageCode = "tr-TR",
                OldSlug = "eski",
                CreatedAt = t0
            });
            // Daha sonra: tekrar slug=eski → yenisi oldu (X→Z, eski-history tekrar)
            seed.SlugHistories.Add(new SlugHistory
            {
                EntityType = SluggedEntityType.Article,
                EntityId = 100,
                LanguageCode = "tr-TR",
                OldSlug = "eski",
                CreatedAt = t1
            });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new SlugHistoryRepository(context);

        var result = await repo.FindCurrentAsync(SluggedEntityType.Article, "tr-TR", "eski");

        result.Should().NotBeNull();
        result!.CreatedAt.Should().BeCloseTo(t1, TimeSpan.FromSeconds(1),
            "CreatedAt DESC.FirstOrDefault en yeni satiri secmeli");
    }

    [Fact]
    public async Task FindCurrentAsync_FiltersByEntityTypeAndLanguage()
    {
        await using (var seed = _factory.CreateContext())
        {
            seed.SlugHistories.AddRange(
                new SlugHistory { EntityType = SluggedEntityType.Article, EntityId = 1, LanguageCode = "tr-TR", OldSlug = "ortak", CreatedAt = DateTime.UtcNow },
                new SlugHistory { EntityType = SluggedEntityType.Page,    EntityId = 2, LanguageCode = "tr-TR", OldSlug = "ortak", CreatedAt = DateTime.UtcNow },
                new SlugHistory { EntityType = SluggedEntityType.Article, EntityId = 3, LanguageCode = "en-US", OldSlug = "ortak", CreatedAt = DateTime.UtcNow });
            await seed.SaveChangesAsync();
        }

        await using var context = _factory.CreateContext();
        var repo = new SlugHistoryRepository(context);

        var article = await repo.FindCurrentAsync(SluggedEntityType.Article, "tr-TR", "ortak");
        var page = await repo.FindCurrentAsync(SluggedEntityType.Page, "tr-TR", "ortak");

        article!.EntityId.Should().Be(1);
        page!.EntityId.Should().Be(2, "EntityType filtresi Page'i Article'dan ayirmali");
    }

    [Fact]
    public async Task AddAsync_PersistsRecord()
    {
        await using var context = _factory.CreateContext();
        var repo = new SlugHistoryRepository(context);

        await repo.AddAsync(new SlugHistory
        {
            EntityType = SluggedEntityType.Page,
            EntityId = 7,
            LanguageCode = "tr-TR",
            OldSlug = "kayit-edildi",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        await using var verify = _factory.CreateContext();
        var saved = await verify.SlugHistories.FirstAsync();
        saved.OldSlug.Should().Be("kayit-edildi");
    }

    public void Dispose() => _factory.Dispose();
}
