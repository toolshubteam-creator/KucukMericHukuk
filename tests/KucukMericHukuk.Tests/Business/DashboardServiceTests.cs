using FluentAssertions;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;

namespace KucukMericHukuk.Tests.Business;

public class DashboardServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;

    public DashboardServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(ArticleMappingConfig).Assembly);
        _mapper = new Mapper(config);
    }

    private DashboardService CreateSut(AppDbContext context)
        => new(new UnitOfWork(context), _mapper);

    private static void SeedArticle(
        AppDbContext context,
        string title,
        string slug,
        ArticleStatus status = ArticleStatus.Published,
        DateTime? publishedAt = null,
        bool isDeleted = false)
    {
        var article = new Article
        {
            Status = status,
            PublishedAt = publishedAt ?? DateTime.UtcNow.AddDays(-1),
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            Translations = new List<ArticleTranslation>
            {
                new()
                {
                    LanguageCode = LanguageCodes.Turkish,
                    Title = title,
                    Slug = slug,
                    Excerpt = $"{title} ozet",
                    Content = $"<p>{title}</p>",
                    ReadingTimeMinutes = 2,
                    CreatedAt = DateTime.UtcNow,
                }
            },
            CreatedAt = DateTime.UtcNow,
        };
        context.Set<Article>().Add(article);
        context.SaveChanges();
    }

    [Fact]
    public async Task GetRecentArticlesAsync_ReturnsPublishedRecent_TotalCountsNonDeleted()
    {
        await using var context = _factory.CreateContext();
        SeedArticle(context, "Yeni", "yeni", ArticleStatus.Published, DateTime.UtcNow.AddDays(-1));
        SeedArticle(context, "Eski", "eski", ArticleStatus.Published, DateTime.UtcNow.AddDays(-5));
        SeedArticle(context, "Taslak", "taslak", ArticleStatus.Draft);
        SeedArticle(context, "Silinmis", "silinmis", ArticleStatus.Published, isDeleted: true);

        var sut = CreateSut(context);
        var result = await sut.GetRecentArticlesAsync(LanguageCodes.Turkish, 5);

        // RecentArticles: yalnızca Published + silinmemiş, PublishedAt DESC
        result.RecentArticles.Should().HaveCount(2);
        result.RecentArticles[0].Title.Should().Be("Yeni");
        result.RecentArticles[1].Title.Should().Be("Eski");
        // TotalCount: silinmemiş tüm makaleler (Published + Draft), soft-delete hariç
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetRecentArticlesAsync_RespectsCountLimit()
    {
        await using var context = _factory.CreateContext();
        for (var i = 0; i < 5; i++)
            SeedArticle(context, $"Makale {i}", $"makale-{i}", publishedAt: DateTime.UtcNow.AddDays(-i));

        var sut = CreateSut(context);
        var result = await sut.GetRecentArticlesAsync(LanguageCodes.Turkish, 3);

        result.RecentArticles.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetSiteSummaryAsync_CountsActiveRecordsPerEntity()
    {
        await using var context = _factory.CreateContext();
        SeedArticle(context, "A1", "a1");
        SeedArticle(context, "A2", "a2");
        SeedArticle(context, "A-silinmis", "a-silinmis", isDeleted: true);
        context.Set<Service>().Add(new Service { CreatedAt = DateTime.UtcNow });
        context.Set<Faq>().Add(new Faq { CreatedAt = DateTime.UtcNow });
        context.Set<Faq>().Add(new Faq { CreatedAt = DateTime.UtcNow });
        context.SaveChanges();

        var sut = CreateSut(context);
        var result = await sut.GetSiteSummaryAsync();

        // Article: soft-delete query filter geçerli → silinmiş hariç 2
        result.ArticleCount.Should().Be(2);
        result.ServiceCount.Should().Be(1);
        result.FaqCount.Should().Be(2);
        // Seed edilmeyen entity'ler 0
        result.AttorneyCount.Should().Be(0);
        result.PageCount.Should().Be(0);
        result.MediaCount.Should().Be(0);
        result.TestimonialCount.Should().Be(0);
    }

    public void Dispose() => _factory.Dispose();
}
