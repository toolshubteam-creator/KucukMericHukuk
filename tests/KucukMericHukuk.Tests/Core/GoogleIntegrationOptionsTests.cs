using FluentAssertions;
using KucukMericHukuk.Core.Common;

namespace KucukMericHukuk.Tests.Core;

public class GoogleIntegrationOptionsTests
{
    [Fact]
    public void HasCredentialSource_AllEmpty_ReturnsFalse()
    {
        var opts = new GoogleIntegrationOptions();

        opts.HasCredentialSource.Should().BeFalse();
    }

    [Theory]
    [InlineData("ServiceAccountJson")]
    [InlineData("ServiceAccountJsonBase64")]
    [InlineData("ServiceAccountFilePath")]
    public void HasCredentialSource_AnySourceSet_ReturnsTrue(string source)
    {
        var opts = new GoogleIntegrationOptions();
        switch (source)
        {
            case "ServiceAccountJson":
                opts.ServiceAccountJson = "{}";
                break;
            case "ServiceAccountJsonBase64":
                opts.ServiceAccountJsonBase64 = "e30=";
                break;
            case "ServiceAccountFilePath":
                opts.ServiceAccountFilePath = "C:\\secure\\google-service-account.json";
                break;
        }

        opts.HasCredentialSource.Should().BeTrue();
    }
}
