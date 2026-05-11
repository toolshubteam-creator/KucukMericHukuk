using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.Infrastructure.Seo;
using Microsoft.Extensions.Options;
using Moq;

namespace KucukMericHukuk.Tests.Infrastructure.Seo;

public class SitemapServiceTests
{
    private readonly SiteInfoOptions _siteInfo = new()
    {
        Name = "Test",
        BaseUrl = "https://example.com"
    };

    private readonly Mock<IArticleRepository> _articles = new();
    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<IAttorneyRepository> _attorneys = new();
    private readonly Mock<IPageRepository> _pages = new();

    private SitemapService Sut() => new(
        Options.Create(_siteInfo),
        _articles.Object, _services.Object, _attorneys.Object, _pages.Object);

    private void SetupAllEmpty()
    {
        _articles.Setup(r => r.GetAllPublishedForSitemapAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Article>());
        _services.Setup(r => r.GetActiveOrderedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Service>());
        _attorneys.Setup(r => r.GetActiveOrderedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Attorney>());
        _pages.Setup(r => r.GetAllActiveForSitemapAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Page>());
    }

    [Fact]
    public async Task BuildSitemapXmlAsync_includes_homepage_and_static_pages()
    {
        SetupAllEmpty();

        var xml = await Sut().BuildSitemapXmlAsync();
        xml.Should().Contain("<loc>https://example.com/tr-TR/</loc>");
        xml.Should().Contain("<loc>https://example.com/tr-TR/Articles</loc>");
        xml.Should().Contain("<loc>https://example.com/tr-TR/Services</loc>");
        xml.Should().Contain("<loc>https://example.com/tr-TR/Attorneys</loc>");
        xml.Should().Contain("<loc>https://example.com/tr-TR/Faqs</loc>");
        xml.Should().Contain("<loc>https://example.com/tr-TR/Contact</loc>");
    }

    [Fact]
    public async Task BuildSitemapXmlAsync_includes_dynamic_articles_and_pages()
    {
        SetupAllEmpty();

        var article = new Article
        {
            Status = ArticleStatus.Published,
            PublishedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Translations = new List<ArticleTranslation>
            {
                new() { LanguageCode = "tr-TR", Slug = "test-makale", Title = "T" }
            }
        };
        _articles.Setup(r => r.GetAllPublishedForSitemapAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Article> { article });

        var page = new Page
        {
            IsActive = true,
            CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            Translations = new List<PageTranslation>
            {
                new() { LanguageCode = "tr-TR", Slug = "hakkimizda", Title = "Hakkımızda" }
            }
        };
        _pages.Setup(r => r.GetAllActiveForSitemapAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Page> { page });

        var xml = await Sut().BuildSitemapXmlAsync();
        xml.Should().Contain("<loc>https://example.com/tr-TR/Articles/test-makale</loc>");
        xml.Should().Contain("<lastmod>2026-05-01</lastmod>");
        xml.Should().Contain("<loc>https://example.com/tr-TR/Pages/hakkimizda</loc>");
        xml.Should().Contain("<lastmod>2026-04-15</lastmod>");
    }

    [Fact]
    public async Task BuildSitemapXmlAsync_skips_translation_without_slug()
    {
        SetupAllEmpty();

        var article = new Article
        {
            Status = ArticleStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Translations = new List<ArticleTranslation>
            {
                new() { LanguageCode = "tr-TR", Slug = "", Title = "T" }
            }
        };
        _articles.Setup(r => r.GetAllPublishedForSitemapAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Article> { article });

        var xml = await Sut().BuildSitemapXmlAsync();
        xml.Should().NotContain("/Articles/");
    }

    [Fact]
    public void BuildRobotsTxt_includes_admin_disallow_and_sitemap_reference()
    {
        var robots = Sut().BuildRobotsTxt();
        robots.Should().Contain("User-agent: *");
        robots.Should().Contain("Disallow: /admin/");
        robots.Should().Contain("Disallow: /Identity/");
        robots.Should().Contain("Disallow: /Contact/ThankYou");
        robots.Should().Contain("Disallow: /Error/");
        robots.Should().Contain("Sitemap: https://example.com/sitemap.xml");
    }
}
