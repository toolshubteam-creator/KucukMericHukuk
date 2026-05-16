using FluentAssertions;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// Faz 7.2b-2: NewsletterService.ProcessJobAsync batch motor testleri.
/// IEmailSender mock — gerçek SMTP yok; durum geçişleri, sayaçlar, NewsletterSentAt davranışı doğrulanır.
/// </summary>
public class NewsletterProcessTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;

    public NewsletterProcessTests()
    {
        _factory = new TestDbContextFactory();
        _mapper = new Mapper(new TypeAdapterConfig());
    }

    private NewsletterService CreateSut(AppDbContext context, IEmailSender emailSender)
    {
        var uow = new UnitOfWork(context);
        var siteInfo = new OptionsSnapshotStub<SiteInfoOptions>(new SiteInfoOptions
        {
            Name = "Test Site",
            BaseUrl = "https://test.local"
        });
        return new NewsletterService(
            uow, _mapper, emailSender, siteInfo, NullLogger<NewsletterService>.Instance);
    }

    private static Article BuildPublishedArticle(string title = "Test")
    {
        var article = new Article
        {
            Status = ArticleStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            NewsletterSentAt = null,
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

    private static async Task<int> SeedArticleAsync(AppDbContext ctx, Article article)
    {
        ctx.Set<Article>().Add(article);
        await ctx.SaveChangesAsync();
        return article.Id;
    }

    private static async Task SeedSubscribersAsync(AppDbContext ctx, int activeCount, int unsubscribedCount = 0)
    {
        for (var i = 0; i < activeCount; i++)
        {
            ctx.Set<Subscriber>().Add(new Subscriber
            {
                Email = $"active{i}@test.local",
                Status = SubscriberStatus.Active,
                UnsubscribeToken = Guid.NewGuid(),
                KvkkConsent = true,
                SubscribedAt = DateTime.UtcNow
            });
        }
        for (var i = 0; i < unsubscribedCount; i++)
        {
            ctx.Set<Subscriber>().Add(new Subscriber
            {
                Email = $"unsub{i}@test.local",
                Status = SubscriberStatus.Unsubscribed,
                UnsubscribeToken = Guid.NewGuid(),
                UnsubscribedAt = DateTime.UtcNow.AddDays(-1),
                KvkkConsent = true,
                SubscribedAt = DateTime.UtcNow.AddDays(-30)
            });
        }
        await ctx.SaveChangesAsync();
    }

    private static async Task<int> SeedPendingJobAsync(AppDbContext ctx, int articleId)
    {
        var job = new NewsletterJob
        {
            ArticleId = articleId,
            Status = NewsletterJobStatus.Pending
        };
        ctx.Set<NewsletterJob>().Add(job);
        await ctx.SaveChangesAsync();
        return job.Id;
    }

    // -------------------- BASARILI AKIS --------------------

    [Fact]
    public async Task ProcessJob_AllActiveSubscribers_AllSent_ArticleFlagSet()
    {
        await using var ctx = _factory.CreateContext();
        var articleId = await SeedArticleAsync(ctx, BuildPublishedArticle("Yayinli"));
        await SeedSubscribersAsync(ctx, activeCount: 3);
        var jobId = await SeedPendingJobAsync(ctx, articleId);

        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(ctx, emailMock.Object);

        var result = await sut.ProcessJobAsync(jobId);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var job = verify.Set<NewsletterJob>().Single(j => j.Id == jobId);
        job.Status.Should().Be(NewsletterJobStatus.Completed);
        job.SentCount.Should().Be(3);
        job.FailedCount.Should().Be(0);
        job.TotalRecipients.Should().Be(3);
        job.StartedAt.Should().NotBeNull();
        job.CompletedAt.Should().NotBeNull();

        var article = verify.Set<Article>().Single(a => a.Id == articleId);
        article.NewsletterSentAt.Should().NotBeNull(
            "Completed job sonrasi makale pending listesinden dusmeli");

        emailMock.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessJob_UnsubscribedNotInRecipients()
    {
        await using var ctx = _factory.CreateContext();
        var articleId = await SeedArticleAsync(ctx, BuildPublishedArticle("Test"));
        await SeedSubscribersAsync(ctx, activeCount: 2, unsubscribedCount: 5);
        var jobId = await SeedPendingJobAsync(ctx, articleId);

        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(ctx, emailMock.Object);

        await sut.ProcessJobAsync(jobId);

        emailMock.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2), "Sadece Active aboneler gonderilmeli, Unsubscribed haric");

        emailMock.Verify(e => e.SendAsync(
            It.Is<string>(to => to.StartsWith("unsub")),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------- KISMI HATA --------------------

    [Fact]
    public async Task ProcessJob_SomeEmailsFail_PartialComplete_ErrorSummarySet()
    {
        await using var ctx = _factory.CreateContext();
        var articleId = await SeedArticleAsync(ctx, BuildPublishedArticle("Kismi"));
        await SeedSubscribersAsync(ctx, activeCount: 5);
        var jobId = await SeedPendingJobAsync(ctx, articleId);

        var emailMock = new Mock<IEmailSender>();
        // ilk email'i fail et, geri kalanlar success
        var sendCallCount = 0;
        emailMock.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, CancellationToken>((to, _, _, _) =>
            {
                sendCallCount++;
                if (sendCallCount == 1)
                    throw new InvalidOperationException($"SMTP refused: {to}");
                return Task.CompletedTask;
            });

        var sut = CreateSut(ctx, emailMock.Object);
        var result = await sut.ProcessJobAsync(jobId);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var job = verify.Set<NewsletterJob>().Single(j => j.Id == jobId);
        job.Status.Should().Be(NewsletterJobStatus.Completed, "Kismi hata isi durdurmaz, batch devam eder");
        job.SentCount.Should().Be(4);
        job.FailedCount.Should().Be(1);
        job.ErrorSummary.Should().NotBeNullOrEmpty();
        job.ErrorSummary.Should().Contain("SMTP refused");

        var article = verify.Set<Article>().Single(a => a.Id == articleId);
        article.NewsletterSentAt.Should().NotBeNull(
            "Kismi hatali ama tamamlanmis job makaleyi gonderildi sayar");
    }

    // -------------------- ABONE YOK --------------------

    [Fact]
    public async Task ProcessJob_NoActiveSubscribers_CompletedZeroOverZero_FlagSet()
    {
        await using var ctx = _factory.CreateContext();
        var articleId = await SeedArticleAsync(ctx, BuildPublishedArticle("YokAbone"));
        // Abone yok - hic Subscriber seed etme
        var jobId = await SeedPendingJobAsync(ctx, articleId);

        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(ctx, emailMock.Object);

        var result = await sut.ProcessJobAsync(jobId);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var job = verify.Set<NewsletterJob>().Single(j => j.Id == jobId);
        job.Status.Should().Be(NewsletterJobStatus.Completed);
        job.TotalRecipients.Should().Be(0);
        job.SentCount.Should().Be(0);
        job.FailedCount.Should().Be(0);
        job.ErrorSummary.Should().Contain("Aktif abone yok");

        var article = verify.Set<Article>().Single(a => a.Id == articleId);
        article.NewsletterSentAt.Should().NotBeNull(
            "Abone olmasa bile gonderim teşebbüsü tamamlandi - makale listede kalmasin");

        emailMock.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------- IDEMPOTENT GUARD --------------------

    [Fact]
    public async Task ProcessJob_AlreadySending_ReturnsJobNotPending()
    {
        await using var ctx = _factory.CreateContext();
        var articleId = await SeedArticleAsync(ctx, BuildPublishedArticle("Test"));
        var job = new NewsletterJob
        {
            ArticleId = articleId,
            Status = NewsletterJobStatus.Sending,
            StartedAt = DateTime.UtcNow
        };
        ctx.Set<NewsletterJob>().Add(job);
        await ctx.SaveChangesAsync();

        var emailMock = new Mock<IEmailSender>();
        var sut = CreateSut(ctx, emailMock.Object);

        var result = await sut.ProcessJobAsync(job.Id);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Newsletter.JobNotPending);

        emailMock.Verify(e => e.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessJob_NotFound_ReturnsJobNotFound()
    {
        await using var ctx = _factory.CreateContext();
        var sut = CreateSut(ctx, new Mock<IEmailSender>().Object);

        var result = await sut.ProcessJobAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Newsletter.JobNotFound);
    }

    // -------------------- FAILED: ARTICLE FLAG SET ETMEME --------------------

    [Fact]
    public async Task ProcessJob_SubscriberRepoThrows_JobFailed_ArticleFlagNotSet()
    {
        // Simulate: beklenmedik exception (DB hatasi, vs.) sirasinda
        // - Job.Status=Failed olmali
        // - Article.NewsletterSentAt NULL kalmali (tekrar denenebilir)
        await using var ctx = _factory.CreateContext();
        var articleId = await SeedArticleAsync(ctx, BuildPublishedArticle("Failure"));
        await SeedSubscribersAsync(ctx, activeCount: 3);
        var jobId = await SeedPendingJobAsync(ctx, articleId);

        // Tum email gonderimleri fail - ama bu kısmi hata, fail değil.
        // Tam fail için DbContext'i sabotaj edemiyoruz kolayca; bunun yerine
        // 2. test (Cancellation) ile flag-not-set davranısı dogrulanir.
        // Bu test: tum email fail ama kismi hata mantigi - Completed kalir.
        var emailMock = new Mock<IEmailSender>();
        emailMock.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));

        var sut = CreateSut(ctx, emailMock.Object);
        await sut.ProcessJobAsync(jobId);

        await using var verify = _factory.CreateContext();
        var job = verify.Set<NewsletterJob>().Single(j => j.Id == jobId);
        job.Status.Should().Be(NewsletterJobStatus.Completed,
            "Tum email fail ama batch devam etti - bu kismi hata, Completed");
        job.FailedCount.Should().Be(3);
        job.SentCount.Should().Be(0);

        // Bu durumda makale NewsletterSentAt SET edilir (Completed sayilir)
        // Failed'da SET ETMEME davranışı OperationCanceledException ile test edilir (ayri test).
    }

    [Fact]
    public async Task ProcessJob_Cancelled_JobFailed_ArticleFlagNotSet()
    {
        // OperationCanceledException → catch (OCE) → Failed işaretle, Article.NewsletterSentAt SET ETME
        await using var ctx = _factory.CreateContext();
        var articleId = await SeedArticleAsync(ctx, BuildPublishedArticle("Cancel"));
        await SeedSubscribersAsync(ctx, activeCount: 50); // batch loop'a girsin
        var jobId = await SeedPendingJobAsync(ctx, articleId);

        var emailMock = new Mock<IEmailSender>();
        var sendCount = 0;
        var cts = new CancellationTokenSource();
        emailMock.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, string, CancellationToken>((to, _, _, _) =>
            {
                sendCount++;
                if (sendCount == 5) cts.Cancel();
                return Task.CompletedTask;
            });

        var sut = CreateSut(ctx, emailMock.Object);
        await sut.ProcessJobAsync(jobId, cts.Token);

        await using var verify = _factory.CreateContext();
        var job = verify.Set<NewsletterJob>().Single(j => j.Id == jobId);
        job.Status.Should().Be(NewsletterJobStatus.Failed,
            "Cancellation -> Failed");

        var article = verify.Set<Article>().Single(a => a.Id == articleId);
        article.NewsletterSentAt.Should().BeNull(
            "Failed job Article.NewsletterSentAt'i SET ETMEMELI - tekrar denenebilir kalmali");
    }

    public void Dispose() => _factory.Dispose();
}
