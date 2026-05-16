using FluentAssertions;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KucukMericHukuk.Tests.Business;

public class NewsletterServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;

    public NewsletterServiceTests()
    {
        _factory = new TestDbContextFactory();
        _mapper = new Mapper(new TypeAdapterConfig());
    }

    private NewsletterService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        return new NewsletterService(uow, _mapper, NullLogger<NewsletterService>.Instance);
    }

    private static Article BuildArticle(
        string title = "Test Makale",
        ArticleStatus status = ArticleStatus.Published,
        DateTime? newsletterSentAt = null,
        DateTime? publishedAt = null)
    {
        var article = new Article
        {
            Status = status,
            PublishedAt = publishedAt ?? (status == ArticleStatus.Published ? DateTime.UtcNow.AddDays(-1) : null),
            NewsletterSentAt = newsletterSentAt,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        article.Translations.Add(new ArticleTranslation
        {
            LanguageCode = LanguageCodes.Default,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(" ", "-"),
            Excerpt = "Ozet",
            Content = "Icerik"
        });
        return article;
    }

    private async Task<int> SeedAsync(AppDbContext ctx, Article article)
    {
        ctx.Set<Article>().Add(article);
        await ctx.SaveChangesAsync();
        return article.Id;
    }

    private async Task<int> SeedActiveSubscribersAsync(AppDbContext ctx, int count)
    {
        for (var i = 0; i < count; i++)
        {
            ctx.Set<Subscriber>().Add(new Subscriber
            {
                Email = $"sub{i}@test.local",
                Status = SubscriberStatus.Active,
                UnsubscribeToken = Guid.NewGuid(),
                KvkkConsent = true,
                SubscribedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
        return count;
    }

    // -------------------- GET PENDING --------------------

    [Fact]
    public async Task GetPendingArticles_PublishedAndNullFlag_ReturnsArticle()
    {
        await using var ctx = _factory.CreateContext();
        await SeedAsync(ctx, BuildArticle("Yeni Yazı"));
        await SeedActiveSubscribersAsync(ctx, 3);

        var sut = CreateSut(ctx);
        var result = await sut.GetPendingArticlesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("Yeni Yazı");
        result.Value[0].ActiveSubscriberCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPendingArticles_PublishedButFlagSet_ExcludedFromPending()
    {
        await using var ctx = _factory.CreateContext();
        await SeedAsync(ctx, BuildArticle("Gonderilmis", newsletterSentAt: DateTime.UtcNow));

        var sut = CreateSut(ctx);
        var result = await sut.GetPendingArticlesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingArticles_DraftArticle_ExcludedFromPending()
    {
        await using var ctx = _factory.CreateContext();
        await SeedAsync(ctx, BuildArticle("Taslak", status: ArticleStatus.Draft));

        var sut = CreateSut(ctx);
        var result = await sut.GetPendingArticlesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // -------------------- CREATE JOB --------------------

    [Fact]
    public async Task CreateJobAsync_ValidPublishedArticle_CreatesPendingJob()
    {
        await using var ctx = _factory.CreateContext();
        var id = await SeedAsync(ctx, BuildArticle("Yayinli"));

        var sut = CreateSut(ctx);
        var result = await sut.CreateJobAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);

        await using var verify = _factory.CreateContext();
        var job = verify.Set<NewsletterJob>().Single(j => j.ArticleId == id);
        job.Status.Should().Be(NewsletterJobStatus.Pending);
        job.SentCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateJobAsync_ArticleNotFound_ReturnsFailure()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx);

        var result = await sut.CreateJobAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Newsletter.ArticleNotFound);
    }

    [Fact]
    public async Task CreateJobAsync_DraftArticle_ReturnsArticleNotPublished()
    {
        await using var ctx = _factory.CreateContext();
        var id = await SeedAsync(ctx, BuildArticle("Taslak", status: ArticleStatus.Draft));

        var sut = CreateSut(ctx);
        var result = await sut.CreateJobAsync(id);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Newsletter.ArticleNotPublished);
    }

    [Fact]
    public async Task CreateJobAsync_AlreadySent_ReturnsAlreadySent()
    {
        await using var ctx = _factory.CreateContext();
        var id = await SeedAsync(ctx, BuildArticle("Gonderilmis", newsletterSentAt: DateTime.UtcNow.AddDays(-1)));

        var sut = CreateSut(ctx);
        var result = await sut.CreateJobAsync(id);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Newsletter.AlreadySent);
    }

    [Fact]
    public async Task CreateJobAsync_DuplicatePendingJob_ReturnsActiveJobExists()
    {
        await using var ctx = _factory.CreateContext();
        var id = await SeedAsync(ctx, BuildArticle("Test"));

        ctx.Set<NewsletterJob>().Add(new NewsletterJob
        {
            ArticleId = id,
            Status = NewsletterJobStatus.Pending
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);
        var result = await sut.CreateJobAsync(id);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Newsletter.ActiveJobExists);
    }

    [Fact]
    public async Task CreateJobAsync_PreviousCompletedJobForSameArticle_DoesNotBlock()
    {
        // Completed/Failed jobs aktif sayılmaz — yeni job acilabilir
        // (Bu PR'da aslında article.NewsletterSentAt set edilince zaten AlreadySent dondurur,
        //  ama defansif test: aktif olmayan job duplicate kontrolunu tetiklemez.)
        await using var ctx = _factory.CreateContext();
        var id = await SeedAsync(ctx, BuildArticle("Test"));

        ctx.Set<NewsletterJob>().Add(new NewsletterJob
        {
            ArticleId = id,
            Status = NewsletterJobStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddDays(-7)
        });
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);
        var result = await sut.CreateJobAsync(id);

        // Article.NewsletterSentAt hala NULL (job Completed ama flag set olmamis - 7.2b-2 yapacak)
        // dolayisiyla yeni job olusturulabilmeli (HasActiveJob false doner)
        result.IsSuccess.Should().BeTrue();
    }

    public void Dispose() => _factory.Dispose();
}
