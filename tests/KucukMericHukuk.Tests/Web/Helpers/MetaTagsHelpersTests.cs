using System.Text.Encodings.Web;
using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Tests.Infrastructure;
using KucukMericHukuk.Web.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace KucukMericHukuk.Tests.Web.Helpers;

public class MetaTagsHelpersTests
{
    private readonly SiteInfoOptions _defaultSiteInfo = new()
    {
        Name = "Test Hukuk",
        Tagline = "Test Slogan",
        Description = "Test description fallback",
        BaseUrl = "https://example.com",
        DefaultOgImage = "/img/og-default.png",
        Locale = "tr_TR",
        TwitterHandle = "@testhukuk"
    };

    /// <summary>
    /// IHtmlHelper mock + ViewData + ViewContext + DI scoped IOptionsSnapshot kurulumu.
    /// Helper iki property kullanır: htmlHelper.ViewData, htmlHelper.ViewContext.HttpContext.
    /// </summary>
    private Mock<IHtmlHelper> CreateHelperMock(
        out ViewDataDictionary viewData,
        string requestPath = "/tr-TR/Test",
        SiteInfoOptions? customSiteInfo = null)
    {
        var siteInfo = customSiteInfo ?? _defaultSiteInfo;

        var services = new ServiceCollection();
        services.AddSingleton<IOptionsSnapshot<SiteInfoOptions>>(
            new OptionsSnapshotStub<SiteInfoOptions>(siteInfo));
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        httpContext.Request.Path = requestPath;

        viewData = new ViewDataDictionary<object>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary());

        var viewContext = new ViewContext
        {
            HttpContext = httpContext,
            ViewData = viewData
        };

        var htmlHelper = new Mock<IHtmlHelper>();
        htmlHelper.Setup(h => h.ViewData).Returns(viewData);
        htmlHelper.Setup(h => h.ViewContext).Returns(viewContext);
        return htmlHelper;
    }

    private static string Render(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    // ─────────────────────── MetaTags() — Title ───────────────────────

    [Fact]
    public void MetaTags_HomePageNoTagline_TitleIsSiteNameOnly()
    {
        var siteInfo = new SiteInfoOptions
        {
            Name = "Test Hukuk",
            Tagline = "",
            BaseUrl = "https://example.com",
            DefaultOgImage = "/img/og.png"
        };
        var helper = CreateHelperMock(out var _, customSiteInfo: siteInfo);

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("<title>Test Hukuk</title>");
        html.Should().NotContain("|");
    }

    [Fact]
    public void MetaTags_HomePageWithTagline_TitleIsNameDashTagline()
    {
        var helper = CreateHelperMock(out var _);

        var html = Render(helper.Object.MetaTags());

        // ASCII-safe substring (— em-dash HTML-encoded olur)
        html.Should().Contain("<title>Test Hukuk");
        html.Should().Contain("Test Slogan</title>");
    }

    [Fact]
    public void MetaTags_NormalPage_TitleIsPipeFormat()
    {
        var helper = CreateHelperMock(out var viewData);
        viewData["Title"] = "Hizmetler";

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("<title>Hizmetler | Test Hukuk</title>");
    }

    // ─────────────────────── MetaTags() — OG image ───────────────────────

    [Fact]
    public void MetaTags_OgImageRelative_PrependsBaseUrl()
    {
        var helper = CreateHelperMock(out var _);
        // ViewData["OgImage"] yok → DefaultOgImage "/img/og-default.png" kullanılır,
        // "http" ile başlamadığı için BaseUrl prepend olur.

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("og:image\" content=\"https://example.com/img/og-default.png\"");
    }

    [Fact]
    public void MetaTags_OgImageAbsoluteUrl_LeavesUnchanged()
    {
        var helper = CreateHelperMock(out var viewData);
        viewData["OgImage"] = "https://cdn.example.com/custom.jpg";

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("og:image\" content=\"https://cdn.example.com/custom.jpg\"");
        html.Should().NotContain("https://example.comhttps://"); // double-prepend olmamalı
    }

    // ─────────────────────── MetaTags() — Canonical ───────────────────────

    [Fact]
    public void MetaTags_CanonicalOverride_UsesViewDataValue()
    {
        var helper = CreateHelperMock(out var viewData);
        viewData["CanonicalUrl"] = "https://example.com/custom-canonical";

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("rel=\"canonical\" href=\"https://example.com/custom-canonical\"");
        html.Should().Contain("og:url\" content=\"https://example.com/custom-canonical\"");
    }

    [Fact]
    public void MetaTags_NoCanonicalOverride_UsesBaseUrlPlusPath()
    {
        var helper = CreateHelperMock(out var _, requestPath: "/tr-TR/makaleler/test");

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("rel=\"canonical\" href=\"https://example.com/tr-TR/makaleler/test\"");
    }

    // ─────────────────────── MetaTags() — Description ───────────────────────

    [Fact]
    public void MetaTags_NoMetaDescription_FallsBackToSiteInfoDescription()
    {
        var helper = CreateHelperMock(out var _);

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("name=\"description\" content=\"Test description fallback\"");
        html.Should().Contain("og:description\" content=\"Test description fallback\"");
    }

    [Fact]
    public void MetaTags_ViewDataMetaDescription_OverridesSiteInfoDescription()
    {
        var helper = CreateHelperMock(out var viewData);
        // Faz 6.16: MinAcceptableLength=80 — primary >=80 char kabul edilir, altı fallback'e düşer.
        // Bu test "primary explicit verilmişse override eder" semantiğini doğrular.
        var explicitDesc = "Bu sayfa için özel ve yeterince uzun bir meta description metni yazıldı, override edilmeli kesinlikle.";
        viewData["MetaDescription"] = explicitDesc;

        var html = Render(helper.Object.MetaTags());

        // CLAUDE.md kuralı: Türkçe karakterler HTML encoded; ASCII-only substring kontrol et.
        html.Should().Contain("yeterince uzun bir meta description metni");
        html.Should().NotContain("Test description fallback");
    }

    // ─────────────────────── MetaTags() — Robots ───────────────────────

    [Fact]
    public void MetaTags_NoRobotsOverride_DefaultsToIndexFollow()
    {
        var helper = CreateHelperMock(out var _);

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("name=\"robots\" content=\"index, follow\"");
    }

    [Fact]
    public void MetaTags_RobotsOverride_UsesViewDataValue()
    {
        var helper = CreateHelperMock(out var viewData);
        viewData["MetaRobots"] = "noindex, nofollow";

        var html = Render(helper.Object.MetaTags());

        html.Should().Contain("name=\"robots\" content=\"noindex, nofollow\"");
        html.Should().NotContain("\"index, follow\"");
    }

    // ─────────────────────── CanonicalForArticles() ───────────────────────

    [Fact]
    public void CanonicalForArticles_NoFilters_ReturnsBasePath()
    {
        var helper = CreateHelperMock(out var _, requestPath: "/tr-TR/makaleler");

        var canonical = helper.Object.CanonicalForArticles(null, 1);

        canonical.Should().Be("https://example.com/tr-TR/makaleler");
    }

    [Fact]
    public void CanonicalForArticles_WithCategoryAndPage_AppendsBothQueryParams()
    {
        var helper = CreateHelperMock(out var _, requestPath: "/tr-TR/makaleler");

        var canonical = helper.Object.CanonicalForArticles("ceza-hukuku", 3);

        canonical.Should().Be("https://example.com/tr-TR/makaleler?category=ceza-hukuku&page=3");
    }

    [Fact]
    public void CanonicalForArticles_PageOne_DoesNotAppendPageQuery()
    {
        var helper = CreateHelperMock(out var _, requestPath: "/tr-TR/makaleler");

        var canonical = helper.Object.CanonicalForArticles("aile-hukuku", 1);

        canonical.Should().Be("https://example.com/tr-TR/makaleler?category=aile-hukuku");
    }

    // ─── Faz 6.16: ResolveDescription / StripHtml / TruncateToSentence ───

    [Fact]
    public void ResolveDescription_PrimaryLongEnough_ReturnsPrimary()
    {
        var primary = new string('a', 140);
        var result = MetaTagsHelpers.ResolveDescription(primary, "fallback", "siteDefault");
        result.Should().Be(primary);
    }

    [Fact]
    public void ResolveDescription_PrimaryTooShort_FallsBackToFallback()
    {
        // Eşik 80 char (MetaTagsHelpers.MinAcceptableLength); fallback bunun üstünde olmalı.
        var fallback = "Bu fallback yeterince uzun bir açıklama metnidir, eşik üstünde kalıyor ve kabul edilmeli açıkça.";
        var result = MetaTagsHelpers.ResolveDescription("Kısa", fallback, "site default");
        result.Should().Be(fallback);
    }

    [Fact]
    public void ResolveDescription_PrimaryNull_UsesFallback()
    {
        var fallback = "Yeterince uzun açıklama içeriği — bu metin meta description için kullanılacak değerdir ve geçerli olmalıdır.";
        var result = MetaTagsHelpers.ResolveDescription(null, fallback, "site default");
        result.Should().Be(fallback);
    }

    [Fact]
    public void ResolveDescription_AllShort_FallsBackToSiteDefault()
    {
        var siteDefault = "Site default metin yeterince uzun olmasa da last-resort olarak kullanılır son çare durumunda hep.";
        var result = MetaTagsHelpers.ResolveDescription("kısa", "yine kısa", siteDefault);
        result.Should().Be(siteDefault);
    }

    [Fact]
    public void ResolveDescription_FallbackHtml_StripsTags()
    {
        var fallback = "<p>Bu <strong>HTML</strong> içerikli bir <a href=\"#\">metindir</a> ve yeterince uzundur, etiketler temizlenmeli üzerinden geçirildiğinde.</p>";
        var result = MetaTagsHelpers.ResolveDescription(null, fallback, "site default");
        result.Should().NotContain("<");
        result.Should().NotContain(">");
        result.Should().Contain("HTML");
    }

    [Fact]
    public void StripHtml_RemovesAllTags()
    {
        var html = "<div><h1>Başlık</h1><p>Paragraf <strong>kalın</strong></p></div>";
        var result = MetaTagsHelpers.StripHtml(html);
        result.Should().Be("Başlık Paragraf kalın");
    }

    [Fact]
    public void StripHtml_DecodesEntities()
    {
        var html = "&amp; &lt;test&gt; &quot;quoted&quot;";
        var result = MetaTagsHelpers.StripHtml(html);
        result.Should().Be("& <test> \"quoted\"");
    }

    [Fact]
    public void TruncateToSentence_ShortText_Unchanged()
    {
        var text = "Kısa metin.";
        var result = MetaTagsHelpers.TruncateToSentence(text, 160);
        result.Should().Be("Kısa metin.");
    }

    [Fact]
    public void TruncateToSentence_PrefersSentenceEnd()
    {
        // Nokta 130. karakterde (>=100 eşiği aşılmış) → cümle sonunda kes
        var text = "İlk cümle yeterince uzun bir metin içeriyor ve burada bitiyor. " +
                   "İkinci cümle de bir devamı olarak ekleniyor ve toplam uzunluk 160 karakteri aşıyor.";
        var result = MetaTagsHelpers.TruncateToSentence(text, 160);
        result.Should().EndWith(".");
        result.Length.Should().BeLessOrEqualTo(160);
    }

    [Fact]
    public void TruncateToSentence_NoDot_FallsBackToSpace()
    {
        // 200 char nokta içermeyen metin → boşlukta kes + …
        var text = new string('a', 80) + " " + new string('b', 80) + " " + new string('c', 80);
        var result = MetaTagsHelpers.TruncateToSentence(text, 160);
        result.Should().EndWith("…");
        result.Length.Should().BeLessOrEqualTo(161); // 160 + "…"
    }
}
