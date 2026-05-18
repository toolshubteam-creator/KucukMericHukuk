using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Middleware;

/// <summary>
/// Faz 7.3.2a — NotFoundLoggingMiddleware POC.
/// KRİTİK ASIL ÖLÇÜ: 404 sonucu NotFoundLogs'a "/tr-TR/Error/404" DEĞİL,
/// orijinal kırık URL ("/tr-TR/bu-sayfa-yok-...") kaydedilmeli (re-execute öncesi).
/// </summary>
public class NotFoundLoggingTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public NotFoundLoggingTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task ClearNotFoundLogsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.NotFoundLogs.RemoveRange(db.NotFoundLogs.ToList());
        await db.SaveChangesAsync();
    }

    private async Task<List<NotFoundLog>> ReadAllAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.NotFoundLogs.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// POC ASIL TESTI: 404 → log'da ORIJINAL URL durmali, re-execute hedefi degil.
    /// IStatusCodeReExecuteFeature.OriginalPath düzgün okunduysa "/tr-TR/yok-..." gelir;
    /// yanlış pipeline order'da "/tr-TR/Error/404" gelir.
    /// </summary>
    [Fact]
    public async Task Unknown_route_logs_ORIGINAL_url_not_error_target()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/bu-sayfa-yok-12345");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var rows = await ReadAllAsync();
        rows.Should().HaveCount(1);
        rows[0].Url.Should().Be("/tr-TR/bu-sayfa-yok-12345",
            "middleware orijinal URL'i kaydetmeli, re-execute hedefini DEGIL");
        rows[0].Url.Should().NotContain("/Error/", "/Error/404 re-execute hedefidir — gurultu");
        rows[0].HitCount.Should().Be(1);
        rows[0].FirstSeenAt.Should().Be(rows[0].LastSeenAt);
    }

    [Fact]
    public async Task Same_unknown_route_hit_twice_aggregates_into_single_row()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        await client.GetAsync("/tr-TR/iki-kez-istenen");
        await Task.Delay(15);
        await client.GetAsync("/tr-TR/iki-kez-istenen");

        var rows = await ReadAllAsync();
        rows.Should().HaveCount(1, "aggregate: ayni URL = tek satir");
        rows[0].HitCount.Should().Be(2);
        rows[0].LastSeenAt.Should().BeAfter(rows[0].FirstSeenAt);
    }

    [Fact]
    public async Task Query_string_is_included_in_logged_url()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        await client.GetAsync("/tr-TR/yok-querystring?utm_source=test&campaign=x");

        var rows = await ReadAllAsync();
        rows.Should().HaveCount(1);
        rows[0].Url.Should().Be("/tr-TR/yok-querystring?utm_source=test&campaign=x",
            "OriginalQueryString feature'dan birlestirilmeli");
    }

    [Fact]
    public async Task Admin_404_is_ignored()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/admin/yok-modul");

        // Admin path 404 dondurebilir (auth/route mismatch), ama log YAZILMAMALI.
        var rows = await ReadAllAsync();
        rows.Should().BeEmpty("/admin/* filtre ile gizlenir");
    }

    [Fact]
    public async Task Error_path_is_ignored()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        await client.GetAsync("/tr-TR/Error/404");

        var rows = await ReadAllAsync();
        rows.Should().BeEmpty("/Error/* re-execute hedefi — log gurultusu");
    }

    [Fact]
    public async Task Asset_extension_is_ignored()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        await client.GetAsync("/tr-TR/yok-stil.css");
        await client.GetAsync("/tr-TR/yok-icon.png");
        await client.GetAsync("/tr-TR/yok-asset.woff2");

        var rows = await ReadAllAsync();
        rows.Should().BeEmpty("statik uzantili 404'ler filtrelenir");
    }

    [Fact]
    public async Task Post_404_is_not_logged()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        await client.PostAsync("/tr-TR/yok-endpoint", content);

        var rows = await ReadAllAsync();
        rows.Should().BeEmpty("POST 404 GET-only filtre ile log'lanmaz");
    }

    [Fact]
    public async Task Long_url_is_truncated_to_max_length()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        // 850 char NotFoundLog.Url sınırını aşan path: prefix + 1000 char segment.
        var longSegment = new string('x', 1000);
        await client.GetAsync($"/tr-TR/{longSegment}");

        var rows = await ReadAllAsync();
        rows.Should().HaveCount(1);
        rows[0].Url.Length.Should().BeLessThanOrEqualTo(850, "middleware truncate yapmali (entity Url NVARCHAR(850))");
        rows[0].Url.Should().StartWith("/tr-TR/xxxx");
    }

    [Fact]
    public async Task Successful_200_request_is_not_logged()
    {
        await ClearNotFoundLogsAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = await ReadAllAsync();
        rows.Should().BeEmpty("200 OK log'lanmaz");
    }
}
