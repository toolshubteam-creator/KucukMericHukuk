using FluentAssertions;
using KucukMericHukuk.Infrastructure.Initialization;

namespace KucukMericHukuk.Tests.Infrastructure;

public class SeedOptionsTests
{
    [Fact]
    public void ValidateForProduction_AllSet_DoesNotThrow()
    {
        var opts = new SeedOptions
        {
            AdminEmail = "admin@kucukmerichukuk.av.tr",
            AdminPassword = "StrongP@ss123",
            AdminFullName = "Sistem Yöneticisi",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateForProduction_AdminEmailEmpty_ThrowsWithMessage()
    {
        var opts = new SeedOptions
        {
            AdminEmail = "",
            AdminPassword = "StrongP@ss123",
            AdminFullName = "Yönetici",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Seed__AdminEmail*");
    }

    [Fact]
    public void ValidateForProduction_AdminEmailNull_ThrowsWithMessage()
    {
        var opts = new SeedOptions
        {
            AdminEmail = null,
            AdminPassword = "StrongP@ss123",
            AdminFullName = "Yönetici",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Seed__AdminEmail*");
    }

    [Fact]
    public void ValidateForProduction_AdminPasswordEmpty_ThrowsWithMessage()
    {
        var opts = new SeedOptions
        {
            AdminEmail = "admin@x.com",
            AdminPassword = "   ", // whitespace-only IsNullOrWhiteSpace == true
            AdminFullName = "Yönetici",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Seed__AdminPassword*");
    }

    [Fact]
    public void ValidateForProduction_AdminFullNameEmpty_ThrowsWithMessage()
    {
        var opts = new SeedOptions
        {
            AdminEmail = "admin@x.com",
            AdminPassword = "StrongP@ss123",
            AdminFullName = "",
        };

        Action act = () => opts.ValidateForProduction();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Seed__AdminFullName*");
    }

    [Fact]
    public void ValidateForProduction_AllEmpty_ListsAllMissingKeys()
    {
        var opts = new SeedOptions();

        Action act = () => opts.ValidateForProduction();
        var ex = act.Should().Throw<InvalidOperationException>().Which;

        ex.Message.Should().Contain("Seed__AdminEmail");
        ex.Message.Should().Contain("Seed__AdminPassword");
        ex.Message.Should().Contain("Seed__AdminFullName");
    }

    [Fact]
    public void ValidateForProduction_PartialMissing_MentionsOnlyMissing()
    {
        var opts = new SeedOptions
        {
            AdminEmail = "admin@x.com",
            // AdminPassword + AdminFullName eksik
        };

        Action act = () => opts.ValidateForProduction();
        var ex = act.Should().Throw<InvalidOperationException>().Which;

        ex.Message.Should().NotContain("Seed__AdminEmail",
            "set edilen alanlar hata mesajında olmamalı");
        ex.Message.Should().Contain("Seed__AdminPassword");
        ex.Message.Should().Contain("Seed__AdminFullName");
    }
}
