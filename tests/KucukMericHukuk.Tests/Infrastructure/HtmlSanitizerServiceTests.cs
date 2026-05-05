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
}
