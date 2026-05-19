using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.Google;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace KucukMericHukuk.Tests.Infrastructure.Google;

public class GoogleSearchConsoleServiceTests
{
    private static GoogleSearchConsoleService CreateSut(
        string? siteUrl = null,
        Result<string>? tokenResult = null,
        ISearchConsoleReportClient? reportClient = null)
    {
        var siteSettings = new Mock<ISiteSettingsService>();
        siteSettings
            .Setup(s => s.GetValueAsync(SiteSettingKeys.GoogleSearchConsoleSiteUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(siteUrl is null
                ? Result.Failure<string?>(new Error(ErrorCodes.SiteSetting.NotFound, "Ayar bulunamadı."))
                : Result.Success<string?>(siteUrl));

        var googleApiClient = new Mock<IGoogleApiClient>();
        googleApiClient
            .Setup(s => s.GetAccessTokenAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResult ?? Result.Failure<string>(
                new Error(ErrorCodes.GoogleIntegration.CredentialMissing, "Credential yok.")));

        return new GoogleSearchConsoleService(
            siteSettings.Object,
            googleApiClient.Object,
            reportClient ?? Mock.Of<ISearchConsoleReportClient>(),
            Options.Create(new GoogleIntegrationOptions()),
            NullLogger<GoogleSearchConsoleService>.Instance);
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_MissingSiteUrl_ReturnsNotConfigured()
    {
        var sut = CreateSut();

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeFalse();
        result.Value.Message.Should().Contain("Site URL");
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_TokenFailure_ReturnsNotConfiguredWithSiteUrl()
    {
        var sut = CreateSut(siteUrl: "https://example.com/");

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeFalse();
        result.Value.SiteUrl.Should().Be("https://example.com/");
        result.Value.Message.Should().Be("Credential yok.");
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_Configured_ReturnsSummaryMetrics()
    {
        var reportClient = new Mock<ISearchConsoleReportClient>();
        reportClient
            .Setup(c => c.GetSummaryAsync(
                "token",
                "https://example.com/",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchConsoleSummary(
                Clicks: 25,
                Impressions: 1000,
                Ctr: 0.025,
                AveragePosition: 7.42));

        var sut = CreateSut(
            siteUrl: "https://example.com/",
            tokenResult: Result.Success("token"),
            reportClient: reportClient.Object);

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.HasData.Should().BeTrue();
        result.Value.Clicks.Should().Be(25);
        result.Value.Impressions.Should().Be(1000);
        result.Value.CtrPercent.Should().Be(2.5);
        result.Value.AveragePosition.Should().Be(7.4);
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_ReportFailure_ReturnsConfiguredEmptyWidget()
    {
        var reportClient = new Mock<ISearchConsoleReportClient>();
        reportClient
            .Setup(c => c.GetSummaryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("api failed"));

        var sut = CreateSut(
            siteUrl: "sc-domain:example.com",
            tokenResult: Result.Success("token"),
            reportClient: reportClient.Object);

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.SiteUrl.Should().Be("sc-domain:example.com");
        result.Value.HasData.Should().BeFalse();
        result.Value.Message.Should().Contain("Search Console verisi alınamadı");
    }
}
