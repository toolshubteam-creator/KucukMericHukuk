using System.Text;
using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.Google;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace KucukMericHukuk.Tests.Infrastructure.Google;

public class GoogleApiClientTests
{
    private static GoogleApiClient CreateSut(
        GoogleIntegrationOptions? options = null,
        string? dbCredentialJson = null)
    {
        var siteSettings = new Mock<ISiteSettingsService>();
        siteSettings
            .Setup(s => s.GetValueAsync(SiteSettingKeys.GoogleServiceAccountJson, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbCredentialJson is null
                ? Result.Failure<string?>(new Error(ErrorCodes.SiteSetting.NotFound, "Ayar bulunamadı."))
                : Result.Success<string?>(dbCredentialJson));

        return new GoogleApiClient(
            Options.Create(options ?? new GoogleIntegrationOptions()),
            siteSettings.Object,
            NullLogger<GoogleApiClient>.Instance);
    }

    [Fact]
    public async Task GetStatusAsync_NoCredentialSource_ReturnsNotConfigured()
    {
        var sut = CreateSut();

        var result = await sut.GetStatusAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeFalse();
        result.Value.CredentialSource.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_ConfigJsonPresent_ReturnsConfigured()
    {
        var sut = CreateSut(new GoogleIntegrationOptions
        {
            ServiceAccountJson = "{}"
        });

        var result = await sut.GetStatusAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.CredentialSource.Should().Be("GoogleIntegration__ServiceAccountJson");
    }

    [Fact]
    public async Task GetStatusAsync_ConfigBase64Present_ReturnsConfigured()
    {
        var json = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
        var sut = CreateSut(new GoogleIntegrationOptions
        {
            ServiceAccountJsonBase64 = json
        });

        var result = await sut.GetStatusAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.CredentialSource.Should().Be("GoogleIntegration__ServiceAccountJsonBase64");
    }

    [Fact]
    public async Task GetStatusAsync_DbCredentialPresent_ReturnsConfigured()
    {
        var sut = CreateSut(dbCredentialJson: "{}");

        var result = await sut.GetStatusAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.CredentialSource.Should().Be("SiteSettings.GoogleServiceAccountJson");
    }

    [Fact]
    public async Task GetAccessTokenAsync_NoCredentialSource_ReturnsCredentialMissing()
    {
        var sut = CreateSut();

        var result = await sut.GetAccessTokenAsync(new[] { "scope" });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.GoogleIntegration.CredentialMissing);
    }

    [Fact]
    public async Task GetAccessTokenAsync_EmptyScopes_ReturnsCredentialInvalid()
    {
        var sut = CreateSut(new GoogleIntegrationOptions
        {
            ServiceAccountJson = "{}"
        });

        var result = await sut.GetAccessTokenAsync(Array.Empty<string>());

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.GoogleIntegration.CredentialInvalid);
    }

    [Fact]
    public async Task GetAccessTokenAsync_InvalidJson_ReturnsTokenRequestFailed()
    {
        var sut = CreateSut(new GoogleIntegrationOptions
        {
            ServiceAccountJson = "{invalid-json"
        });

        var result = await sut.GetAccessTokenAsync(new[] { "scope" });

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.GoogleIntegration.TokenRequestFailed);
    }
}
