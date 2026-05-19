using FluentAssertions;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Infrastructure.Google;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace KucukMericHukuk.Tests.Infrastructure.Google;

public class GoogleAnalyticsServiceTests
{
    private static GoogleAnalyticsService CreateSut(
        string? propertyId = null,
        Result<string>? tokenResult = null,
        IAnalyticsDataReportClient? reportClient = null)
    {
        var siteSettings = new Mock<ISiteSettingsService>();
        siteSettings
            .Setup(s => s.GetValueAsync(SiteSettingKeys.GoogleAnalyticsPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyId is null
                ? Result.Failure<string?>(new Error(ErrorCodes.SiteSetting.NotFound, "Ayar bulunamadı."))
                : Result.Success<string?>(propertyId));

        var googleApiClient = new Mock<IGoogleApiClient>();
        googleApiClient
            .Setup(s => s.GetAccessTokenAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResult ?? Result.Failure<string>(
                new Error(ErrorCodes.GoogleIntegration.CredentialMissing, "Credential yok.")));

        return new GoogleAnalyticsService(
            siteSettings.Object,
            googleApiClient.Object,
            reportClient ?? Mock.Of<IAnalyticsDataReportClient>(),
            Options.Create(new GoogleIntegrationOptions()),
            NullLogger<GoogleAnalyticsService>.Instance);
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_MissingPropertyId_ReturnsNotConfigured()
    {
        var sut = CreateSut();

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeFalse();
        result.Value.Message.Should().Contain("Property ID");
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_TokenFailure_ReturnsNotConfiguredWithPropertyId()
    {
        var sut = CreateSut(propertyId: "123456");

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeFalse();
        result.Value.PropertyId.Should().Be("123456");
        result.Value.Message.Should().Be("Credential yok.");
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_Configured_ReturnsSummaryMetrics()
    {
        var reportClient = new Mock<IAnalyticsDataReportClient>();
        reportClient
            .Setup(c => c.GetSummaryAsync(
                "token",
                "properties/123456",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AnalyticsDataSummary(
                ActiveUsers: 10,
                NewUsers: 8,
                Sessions: 14,
                ScreenPageViews: 30,
                EventCount: 55));

        var sut = CreateSut(
            propertyId: "123456",
            tokenResult: Result.Success("token"),
            reportClient: reportClient.Object);

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.HasData.Should().BeTrue();
        result.Value.ActiveUsers.Should().Be(10);
        result.Value.NewUsers.Should().Be(8);
        result.Value.Sessions.Should().Be(14);
        result.Value.ScreenPageViews.Should().Be(30);
        result.Value.EventCount.Should().Be(55);
    }

    [Fact]
    public async Task GetDashboardWidgetAsync_ReportFailure_ReturnsConfiguredEmptyWidget()
    {
        var reportClient = new Mock<IAnalyticsDataReportClient>();
        reportClient
            .Setup(c => c.GetSummaryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("api failed"));

        var sut = CreateSut(
            propertyId: "properties/123456",
            tokenResult: Result.Success("token"),
            reportClient: reportClient.Object);

        var result = await sut.GetDashboardWidgetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfigured.Should().BeTrue();
        result.Value.PropertyId.Should().Be("properties/123456");
        result.Value.HasData.Should().BeFalse();
        result.Value.Message.Should().Contain("GA4 verisi alınamadı");
    }
}
