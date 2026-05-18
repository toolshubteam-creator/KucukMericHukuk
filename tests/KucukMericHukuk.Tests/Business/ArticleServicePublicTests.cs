using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Infrastructure.Security;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;

namespace KucukMericHukuk.Tests.Business;

public class ArticleServicePublicTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<ArticleInputDto> _validator;

    public ArticleServicePublicTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(ArticleMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new ArticleInputValidator();
    }

    private ArticleService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var slugService = new SlugService(uow);
        var slugHistoryService = new SlugHistoryService(uow);
        var sanitizer = new HtmlSanitizerService();
        return new ArticleService(uow, slugService, slugHistoryService, _mapper, _validator, sanitizer);
    }

    private static int SeedArticle(
        AppDbContext context,
        string title,
        string slug,
        ArticleStatus status = ArticleStatus.Published,
        bool isFeatured = false,
        DateTime? publishedAt = null,
        int? categoryId = null)
    {
        var article = new Article
        {
            CategoryId = categoryId,
            Status = status,
            IsFeatured = isFeatured,
            PublishedAt = publishedAt ?? DateTime.UtcNow.AddDays(-1),
            Translations = new List<ArticleTranslation>
            {
                new()
                {
                    LanguageCode = LanguageCodes.Turkish,
                    Title = title,
                    Slug = slug,
                    Excerpt = $"{title} özet",
                    Content = $"<p>{title} içerik</p>",
                    ReadingTimeMinutes = 3,
                    CreatedAt = DateTime.UtcNow,
                }
            },
            CreatedAt = DateTime.UtcNow,
        };
        context.Set<Article>().Add(article);
        context.SaveChanges();
        return article.Id;
    }

    private static int SeedCategory(AppDbContext context, string name, string slug)
    {
        var cat = new Category
        {
            IsActive = true,
            Translations = new List<CategoryTranslation>
            {
                new()
                {
                    LanguageCode = LanguageCodes.Turkish,
                    Name = name,
                    Slug = slug,
                    CreatedAt = DateTime.UtcNow,
                }
            },
            CreatedAt = DateTime.UtcNow,
        };
        context.Set<Category>().Add(cat);
        context.SaveChanges();
        return cat.Id;
    }

    [Fact]
    public async Task GetBySlugAsync_SlugFound_IncrementsViewCountAndReturnsDto()
    {
        await using var context = _factory.CreateContext();
        var id = SeedArticle(context, "Test Makale", "test-makale");
        var sut = CreateSut(context);

        var result = await sut.GetBySlugAsync(LanguageCodes.Turkish, "test-makale");

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test Makale");
        result.Value.Slug.Should().Be("test-makale");

        await using var verify = _factory.CreateContext();
        var persisted = verify.Set<Article>().First(a => a.Id == id);
        persisted.ViewCount.Should().Be(1);
    }

    [Fact]
    public async Task GetBySlugAsync_SlugNotFound_ReturnsFailure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetBySlugAsync(LanguageCodes.Turkish, "yok-boyle-slug");

        result.IsSuccess.Should().BeFalse();
        result.FirstError.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRelatedAsync_SameCategoryHasEnough_ReturnsOnlyFromCategory_ExcludesCurrent()
    {
        await using var context = _factory.CreateContext();
        var catId = SeedCategory(context, "Aile Hukuku", "aile-hukuku");
        var otherCatId = SeedCategory(context, "Ceza Hukuku", "ceza-hukuku");

        var current = SeedArticle(context, "Current", "current", categoryId: catId);
        var sameA = SeedArticle(context, "SameA", "samea", categoryId: catId, publishedAt: DateTime.UtcNow.AddDays(-2));
        var sameB = SeedArticle(context, "SameB", "sameb", categoryId: catId, publishedAt: DateTime.UtcNow.AddDays(-3));
        var sameC = SeedArticle(context, "SameC", "samec", categoryId: catId, publishedAt: DateTime.UtcNow.AddDays(-4));
        SeedArticle(context, "OtherX", "otherx", categoryId: otherCatId);

        var sut = CreateSut(context);

        var result = await sut.GetRelatedAsync(current, catId, LanguageCodes.Turkish, count: 3);

        result.Should().HaveCount(3);
        result.Select(r => r.Slug).Should().BeEquivalentTo(new[] { "samea", "sameb", "samec" });
        result.Should().NotContain(r => r.Slug == "current");
        result.Should().NotContain(r => r.Slug == "otherx");
    }

    [Fact]
    public async Task GetRelatedAsync_SameCategoryEmpty_FallsBackToRecent()
    {
        await using var context = _factory.CreateContext();
        var catId = SeedCategory(context, "Aile Hukuku", "aile-hukuku");
        var otherCatId = SeedCategory(context, "Ceza Hukuku", "ceza-hukuku");

        var current = SeedArticle(context, "Current", "current", categoryId: catId);
        SeedArticle(context, "RecentX", "recentx", categoryId: otherCatId, publishedAt: DateTime.UtcNow.AddDays(-2));
        SeedArticle(context, "RecentY", "recenty", categoryId: otherCatId, publishedAt: DateTime.UtcNow.AddDays(-3));
        SeedArticle(context, "RecentZ", "recentz", categoryId: null, publishedAt: DateTime.UtcNow.AddDays(-4));

        var sut = CreateSut(context);

        var result = await sut.GetRelatedAsync(current, catId, LanguageCodes.Turkish, count: 3);

        result.Should().HaveCount(3);
        result.Should().NotContain(r => r.Slug == "current");
        result.Select(r => r.Slug).Should().BeEquivalentTo(new[] { "recentx", "recenty", "recentz" });
    }

    [Fact]
    public async Task GetByCategoryAsync_FiltersByCategoryAndPaginates()
    {
        await using var context = _factory.CreateContext();
        var aileId = SeedCategory(context, "Aile Hukuku", "aile-hukuku");
        var cezaId = SeedCategory(context, "Ceza Hukuku", "ceza-hukuku");

        SeedArticle(context, "Aile1", "aile1", categoryId: aileId);
        SeedArticle(context, "Aile2", "aile2", categoryId: aileId);
        SeedArticle(context, "Ceza1", "ceza1", categoryId: cezaId);

        var sut = CreateSut(context);

        var aileResult = await sut.GetByCategoryAsync(aileId, LanguageCodes.Turkish, 1, 10);

        aileResult.TotalCount.Should().Be(2);
        aileResult.Items.Select(i => i.Slug).Should().BeEquivalentTo(new[] { "aile1", "aile2" });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
