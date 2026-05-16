using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KucukMericHukuk.IntegrationTests.Newsletter;

/// <summary>
/// Faz 7.2b-2: SendJob endpoint + Dispatcher entegrasyonu.
/// IEmailSender DI override (Mock) — gerçek SMTP yok. Dispatcher fire-and-forget olduğu için
/// gönderim tamamlanmasını bekleriz (polling) sonra DB durumunu doğrularız.
/// </summary>
public class NewsletterSendTests : IClassFixture<NewsletterSendTestFactory>
{
    private readonly NewsletterSendTestFactory _factory;

    public NewsletterSendTests(NewsletterSendTestFactory factory)
    {
        _factory = factory;
    }

    private async Task ResetDbAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<NewsletterJob>().RemoveRange(db.Set<NewsletterJob>().IgnoreQueryFilters().ToList());
        db.Set<Article>().RemoveRange(db.Set<Article>().IgnoreQueryFilters().ToList());
        db.Set<Subscriber>().RemoveRange(db.Set<Subscriber>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();
    }

    private async Task<(int articleId, int jobId)> SeedPendingJobWithSubscribersAsync(int activeSubscribers)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var article = new Article
        {
            Status = ArticleStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        article.Translations.Add(new ArticleTranslation
        {
            LanguageCode = LanguageCodes.Default,
            Title = "Integration Send Test",
            Slug = "integration-send-test",
            Excerpt = "Ozet",
            Content = "Icerik"
        });
        db.Set<Article>().Add(article);
        await db.SaveChangesAsync();

        for (var i = 0; i < activeSubscribers; i++)
        {
            db.Set<Subscriber>().Add(new Subscriber
            {
                Email = $"sub{i}@test.local",
                Status = SubscriberStatus.Active,
                UnsubscribeToken = Guid.NewGuid(),
                KvkkConsent = true,
                SubscribedAt = DateTime.UtcNow
            });
        }

        var job = new NewsletterJob { ArticleId = article.Id, Status = NewsletterJobStatus.Pending };
        db.Set<NewsletterJob>().Add(job);
        await db.SaveChangesAsync();

        return (article.Id, job.Id);
    }

    private async Task<NewsletterJob?> WaitForJobAsync(
        int jobId, NewsletterJobStatus target, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = await db.Set<NewsletterJob>().AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId);
            if (job?.Status == target) return job;
            await Task.Delay(200);
        }
        return null;
    }

    [Fact]
    public async Task PostSendJob_PendingJob_DispatchesAndCompletes()
    {
        await ResetDbAsync();
        var (articleId, jobId) = await SeedPendingJobWithSubscribersAsync(activeSubscribers: 2);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/newsletter");

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await client.PostAsync($"/admin/newsletter/send-job/{jobId}", form);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        // Dispatcher fire-and-forget — polling ile Completed bekle
        var completed = await WaitForJobAsync(jobId, NewsletterJobStatus.Completed, TimeSpan.FromSeconds(30));
        completed.Should().NotBeNull("background dispatcher Pending -> Completed yapmali");

        completed!.SentCount.Should().Be(2);
        completed.FailedCount.Should().Be(0);
        completed.TotalRecipients.Should().Be(2);

        // Mock IEmailSender 2 kez cagrildi mi
        _factory.EmailMock.Verify(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));

        // Article.NewsletterSentAt set edilmiş olmalı
        using var verify = _factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var article = await db.Set<Article>().AsNoTracking().FirstAsync(a => a.Id == articleId);
        article.NewsletterSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PostSendJob_AlreadySending_Rejected()
    {
        await ResetDbAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var article = new Article
            {
                Status = ArticleStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            article.Translations.Add(new ArticleTranslation
            {
                LanguageCode = LanguageCodes.Default,
                Title = "Sending Test",
                Slug = "sending-test",
                Content = "."
            });
            db.Set<Article>().Add(article);
            await db.SaveChangesAsync();

            db.Set<NewsletterJob>().Add(new NewsletterJob
            {
                ArticleId = article.Id,
                Status = NewsletterJobStatus.Sending,
                StartedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        int jobId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            jobId = db.Set<NewsletterJob>().Single().Id;
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/newsletter");

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await client.PostAsync($"/admin/newsletter/send-job/{jobId}", form);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);

        // Status degismedi - hala Sending (controller dispatch yapmadi)
        using var verify = _factory.Services.CreateScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db2.Set<NewsletterJob>().AsNoTracking().FirstAsync(j => j.Id == jobId);
        job.Status.Should().Be(NewsletterJobStatus.Sending, "Sending job dispatch reddedilmeli");
    }
}
