using FluentAssertions;
using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Tests.Core;

/// <summary>
/// Faz 6.25 — Turnstile production enforcement. Pattern: SeedOptionsTests sablon alindi
/// (parametre-siz ValidateForProduction, caller Program.cs'te IsProduction() ile sarar).
/// </summary>
public class TurnstileOptionsTests
{
    [Fact]
    public void ValidateForProduction_AllSet_DoesNotThrow()
    {
        var opts = new TurnstileOptions
        {
            SiteKey = "0x1234567890abcdef",
            SecretKey = "0x9876543210fedcba",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateForProduction_SiteKeyEmpty_ThrowsWithMessage()
    {
        var opts = new TurnstileOptions
        {
            SiteKey = "",
            SecretKey = "0x9876543210fedcba",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Turnstile__SiteKey*");
    }

    [Fact]
    public void ValidateForProduction_SiteKeyWhitespace_ThrowsWithMessage()
    {
        var opts = new TurnstileOptions
        {
            SiteKey = "   ", // whitespace-only IsNullOrWhiteSpace == true
            SecretKey = "0x9876543210fedcba",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Turnstile__SiteKey*");
    }

    [Fact]
    public void ValidateForProduction_SecretKeyEmpty_ThrowsWithMessage()
    {
        var opts = new TurnstileOptions
        {
            SiteKey = "0x1234567890abcdef",
            SecretKey = "",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Turnstile__SecretKey*");
    }

    [Fact]
    public void ValidateForProduction_BothEmpty_ListsBothMissingKeys()
    {
        var opts = new TurnstileOptions
        {
            SiteKey = "",
            SecretKey = "",
        };

        Action act = () => opts.ValidateForProduction();
        var ex = act.Should().Throw<InvalidOperationException>().Which;

        ex.Message.Should().Contain("Turnstile__SiteKey");
        ex.Message.Should().Contain("Turnstile__SecretKey");
    }

    [Fact]
    public void ValidateForProduction_PartialMissing_MentionsOnlyMissing()
    {
        var opts = new TurnstileOptions
        {
            SiteKey = "0x1234567890abcdef",
            SecretKey = "", // sadece secret eksik
        };

        Action act = () => opts.ValidateForProduction();
        var ex = act.Should().Throw<InvalidOperationException>().Which;

        ex.Message.Should().NotContain("Turnstile__SiteKey",
            "set edilen alanlar hata mesajinda olmamali");
        ex.Message.Should().Contain("Turnstile__SecretKey");
    }
}
