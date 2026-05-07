using FluentAssertions;
using KucukMericHukuk.Infrastructure.Security;

namespace KucukMericHukuk.Tests.Infrastructure;

public class HtmlSanitizerServiceTests
{
    private readonly HtmlSanitizerService _sut = new();

    [Fact]
    public void Sanitize_PlainText_ShouldReturnAsIs()
    {
        _sut.Sanitize("Merhaba dünya").Should().Be("Merhaba dünya");
    }

    [Fact]
    public void Sanitize_AllowedTags_ShouldKeep()
    {
        var result = _sut.Sanitize("<p>İçerik</p>");
        result.Should().Contain("<p>").And.Contain("</p>").And.Contain("İçerik");
    }

    [Fact]
    public void Sanitize_ScriptTag_ShouldRemove()
    {
        var result = _sut.Sanitize("<p>OK</p><script>alert('xss')</script>");
        result.Should().NotContain("<script");
        result.Should().NotContain("alert");
        result.Should().Contain("<p>OK</p>");
    }

    [Fact]
    public void Sanitize_OnAttribute_ShouldRemove()
    {
        var result = _sut.Sanitize("<a href=\"/x\" onclick=\"alert(1)\">x</a>");
        result.Should().NotContain("onclick");
        result.Should().Contain("href");
    }

    [Fact]
    public void Sanitize_JavascriptUrl_ShouldRemove()
    {
        var result = _sut.Sanitize("<a href=\"javascript:alert(1)\">x</a>");
        result.Should().NotContain("javascript:");
    }

    [Fact]
    public void Sanitize_StyleAttribute_ShouldRemove()
    {
        var result = _sut.Sanitize("<p style=\"color:red\">X</p>");
        result.Should().NotContain("style=");
        result.Should().Contain("<p>").And.Contain("X");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_NullOrWhitespace_ShouldReturnEmpty(string? input)
    {
        _sut.Sanitize(input).Should().Be(string.Empty);
    }

    [Fact]
    public void Sanitize_AllowedScheme_HttpsLink_ShouldKeep()
    {
        var result = _sut.Sanitize("<a href=\"https://example.com\">OK</a>");
        result.Should().Contain("https://example.com");
    }

    [Fact]
    public void Sanitize_DisallowedScheme_FtpLink_ShouldRemoveHref()
    {
        var result = _sut.Sanitize("<a href=\"ftp://x\">X</a>");
        result.Should().NotContain("ftp://");
    }

    [Fact]
    public void Sanitize_ImgTag_WithHttpsSrc_ShouldKeep()
    {
        var input = "<p>Önce</p><img src=\"https://example.com/foto.webp\" alt=\"foto\" width=\"600\" height=\"400\" class=\"img-fluid\" /><p>Sonra</p>";
        var result = _sut.Sanitize(input);
        result.Should().Contain("<img");
        result.Should().Contain("src=\"https://example.com/foto.webp\"");
        result.Should().Contain("alt=\"foto\"");
        result.Should().Contain("width=\"600\"");
        result.Should().Contain("height=\"400\"");
        result.Should().Contain("class=\"img-fluid\"");
    }

    [Fact]
    public void Sanitize_ImgTag_WithHttpSrc_ShouldStripSrc()
    {
        // http reddedildiği için src URL DOM'a yansımamalı (Ganss.Xss: scheme reject → strip).
        var input = "<img src=\"http://insecure.example.com/x.jpg\" alt=\"x\" />";
        var result = _sut.Sanitize(input);
        result.Should().NotContain("http://insecure.example.com");
    }

    [Fact]
    public void Sanitize_ImgTag_WithJavascriptSrc_ShouldStrip()
    {
        var input = "<img src=\"javascript:alert(1)\" alt=\"xss\" />";
        var result = _sut.Sanitize(input);
        result.Should().NotContain("javascript:");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void Sanitize_ImgTag_WithStyleAttribute_ShouldStripStyle()
    {
        // style izin verilmedi (Faz 3.3 kararı) — strip edilmeli, ama img + diğer attribute'lar kalır.
        var input = "<img src=\"https://example.com/x.webp\" alt=\"x\" style=\"position:absolute; top:0\" />";
        var result = _sut.Sanitize(input);
        result.Should().Contain("<img");
        result.Should().Contain("https://example.com/x.webp");
        result.Should().NotContain("style");
        result.Should().NotContain("position");
    }

    [Fact]
    public void Sanitize_ImgTag_WithOnerrorAttribute_ShouldStrip()
    {
        var input = "<img src=\"https://example.com/x.webp\" onerror=\"alert(1)\" />";
        var result = _sut.Sanitize(input);
        result.Should().NotContain("onerror");
        result.Should().NotContain("alert");
    }
}
