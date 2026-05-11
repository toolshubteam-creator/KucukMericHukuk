using System.Text.Json;
using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Infrastructure.Seo;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Tests.Infrastructure.Seo;

public class JsonLdServiceTests
{
    private readonly JsonLdService _sut;
    private readonly SiteInfoOptions _siteInfo;

    public JsonLdServiceTests()
    {
        _siteInfo = new SiteInfoOptions
        {
            Name = "Küçükmeriç Hukuk Bürosu",
            BaseUrl = "https://kucukmerichukuk.av.tr",
            Description = "Test description",
            DefaultOgImage = "/img/og-default.png",
            Telephone = "+902644000000",
            Email = "info@example.com",
            StreetAddress = "Atatürk Cad. No:1",
            AddressLocality = "Serdivan",
            AddressRegion = "Sakarya",
            AddressCountry = "TR",
            AreaServed = "Sakarya, Türkiye"
        };
        _sut = new JsonLdService(Options.Create(_siteInfo));
    }

    [Fact]
    public void BuildLegalService_includes_required_properties()
    {
        var json = _sut.BuildLegalService();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("@context").GetString().Should().Be("https://schema.org");
        root.GetProperty("@type").GetString().Should().Be("LegalService");
        root.GetProperty("name").GetString().Should().Be(_siteInfo.Name);
        root.GetProperty("url").GetString().Should().Be(_siteInfo.BaseUrl);
        root.GetProperty("telephone").GetString().Should().Be(_siteInfo.Telephone);
        root.GetProperty("address").GetProperty("@type").GetString().Should().Be("PostalAddress");
        root.GetProperty("address").GetProperty("addressLocality").GetString().Should().Be("Serdivan");
    }

    [Fact]
    public void BuildLegalService_omits_empty_properties()
    {
        var emptyOpts = new SiteInfoOptions { Name = "X", BaseUrl = "https://x.tr" };
        var sut = new JsonLdService(Options.Create(emptyOpts));
        var json = sut.BuildLegalService();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("telephone", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("email", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("geo", out _).Should().BeFalse();
    }

    [Fact]
    public void BuildWebSite_returns_valid_schema()
    {
        var json = _sut.BuildWebSite();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("@type").GetString().Should().Be("WebSite");
        doc.RootElement.GetProperty("inLanguage").GetString().Should().Be("tr-TR");
    }

    [Fact]
    public void BuildArticle_includes_author_and_absolute_image()
    {
        var article = new ArticleDetailDto
        {
            Title = "Test Article",
            Slug = "test-article",
            Excerpt = "Excerpt",
            FeaturedImageUrl = "/media/x.jpg",
            PublishedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            AuthorName = "Av. Test"
        };
        var json = _sut.BuildArticle(article);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("@type").GetString().Should().Be("Article");
        doc.RootElement.GetProperty("headline").GetString().Should().Be("Test Article");
        doc.RootElement.GetProperty("image").GetString()
            .Should().Be("https://kucukmerichukuk.av.tr/media/x.jpg");
        doc.RootElement.GetProperty("author").GetProperty("name").GetString().Should().Be("Av. Test");
        doc.RootElement.GetProperty("datePublished").GetString().Should().Contain("2026-05-01");
    }

    [Fact]
    public void BuildPerson_includes_jobTitle_and_worksFor()
    {
        var attorney = new AttorneyDetailDto
        {
            FullName = "Av. Demo",
            Slug = "av-demo",
            Title = "Kıdemli Avukat",
            ShortBio = "Bio",
            ProfileImageUrl = "/media/p.jpg"
        };
        var json = _sut.BuildPerson(attorney);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("@type").GetString().Should().Be("Person");
        doc.RootElement.GetProperty("name").GetString().Should().Be("Av. Demo");
        doc.RootElement.GetProperty("jobTitle").GetString().Should().Be("Kıdemli Avukat");
        doc.RootElement.GetProperty("worksFor").GetProperty("name").GetString().Should().Be(_siteInfo.Name);
    }

    [Fact]
    public void BuildBreadcrumbList_returns_empty_for_no_items()
    {
        var json = _sut.BuildBreadcrumbList(Array.Empty<(string, string?)>());
        json.Should().BeEmpty();
    }

    [Fact]
    public void BuildBreadcrumbList_includes_position_starting_at_one()
    {
        var items = new (string, string?)[]
        {
            ("Ana Sayfa", "/tr-TR/"),
            ("Makaleler", "/tr-TR/Articles"),
            ("Test Makale", null)
        };
        var json = _sut.BuildBreadcrumbList(items);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("@type").GetString().Should().Be("BreadcrumbList");
        var elements = doc.RootElement.GetProperty("itemListElement");
        elements.GetArrayLength().Should().Be(3);
        elements[0].GetProperty("position").GetInt32().Should().Be(1);
        elements[0].GetProperty("name").GetString().Should().Be("Ana Sayfa");
        elements[2].GetProperty("position").GetInt32().Should().Be(3);
        elements[2].GetProperty("name").GetString().Should().Be("Test Makale");
    }

    [Fact]
    public void BuildBreadcrumbList_omits_item_url_when_null_and_makes_others_absolute()
    {
        var items = new (string, string?)[]
        {
            ("Ana Sayfa", "/tr-TR/"),
            ("Current Page", null)
        };
        var json = _sut.BuildBreadcrumbList(items);
        using var doc = JsonDocument.Parse(json);
        var elements = doc.RootElement.GetProperty("itemListElement");

        elements[0].GetProperty("item").GetString()
            .Should().Be("https://kucukmerichukuk.av.tr/tr-TR/");
        elements[1].TryGetProperty("item", out _).Should().BeFalse();
    }

    [Fact]
    public void BuildFaqPage_serializes_question_array()
    {
        var faqs = new List<FaqListDto>
        {
            new() { Question = "Q1", Answer = "A1" },
            new() { Question = "Q2", Answer = "A2" }
        };
        var json = _sut.BuildFaqPage(faqs);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("@type").GetString().Should().Be("FAQPage");
        var entities = doc.RootElement.GetProperty("mainEntity");
        entities.GetArrayLength().Should().Be(2);
        entities[0].GetProperty("name").GetString().Should().Be("Q1");
        entities[0].GetProperty("acceptedAnswer").GetProperty("text").GetString().Should().Be("A1");
    }
}
