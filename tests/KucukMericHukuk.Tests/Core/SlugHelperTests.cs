using FluentAssertions;
using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Tests.Core;

public class SlugHelperTests
{
    [Theory]
    [InlineData("İcra Hukuku", "icra-hukuku")]
    [InlineData("Şirket & Ticaret", "sirket-ticaret")]
    [InlineData("Çocuk Hukuku ÖZEL", "cocuk-hukuku-ozel")]
    [InlineData("Ğarip Ünlü Öteki", "garip-unlu-oteki")]
    [InlineData("  Boşluklar   ", "bosluklar")]
    [InlineData("Çoklu---tire", "coklu-tire")]
    [InlineData("Sayı123 Var", "sayi123-var")]
    [InlineData("---başta-sonda---", "basta-sonda")]
    [InlineData("Iki Ihtimal İki", "iki-ihtimal-iki")]
    [InlineData("ısı ölçer", "isi-olcer")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("!!!@@@###", "")]
    public void Generate_ShouldProduceExpectedSlug(string? input, string expected)
    {
        SlugHelper.Generate(input).Should().Be(expected);
    }

    [Fact]
    public void Generate_LongInput_ShouldRespectMaxLength()
    {
        var longInput = new string('a', 300);

        var result = SlugHelper.Generate(longInput, maxLength: 50);

        result.Length.Should().BeLessThanOrEqualTo(50);
        result.Should().NotEndWith("-");
    }

    [Fact]
    public void Generate_TruncationLandingOnHyphen_ShouldTrimTrailingHyphen()
    {
        // "icra-hukuku-takipler" — 20 char total. maxLength=12 keser "icra-hukuku-" → trailing - kaldırılmalı
        var result = SlugHelper.Generate("İcra Hukuku Takipler", maxLength: 12);

        result.Should().Be("icra-hukuku");
        result.Should().NotEndWith("-");
    }
}
