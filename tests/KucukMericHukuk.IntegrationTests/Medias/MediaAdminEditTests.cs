using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Medias;

/// <summary>
/// Regresyon: 6.5'te Edit form'unda manuel hidden field + asp-for auto hidden çakışması
/// IsPublic'i her zaman false yapıyordu. Bu testler aynı bug'ı tekrar yakalar.
/// </summary>
public class MediaAdminEditTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public MediaAdminEditTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> SeedMediaAsync(bool isPublic, string? altText = "test alt")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Temizle (paralel testlerle çakışmasın)
        db.Set<MediaFile>().RemoveRange(db.Set<MediaFile>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();

        var entity = new MediaFile
        {
            FileName = "test.webp",
            OriginalFileName = "test.jpg",
            RelativePath = "uploads/2026/05/test.webp",
            ThumbnailRelativePath = "uploads/2026/05/test_thumb.webp",
            ContentType = "image/webp",
            FileSizeBytes = 1024,
            Width = 800,
            Height = 600,
            Sha256 = "edittest" + new string('0', 56),
            AltText = altText,
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow,
        };

        db.Set<MediaFile>().Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    private async Task<bool> ReadIsPublicAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.Set<MediaFile>().FirstAsync(m => m.Id == id);
        return entity.IsPublic;
    }

    /// <summary>
    /// Regresyon koruması: Edit form HTML'de IsPublic için TAM 2 input olmalı
    /// (asp-for'un ürettiği checkbox + auto hidden). 3 olursa manuel hidden eklenmiş demektir → bug.
    /// </summary>
    [Fact]
    public async Task EditForm_RendersExactlyTwoIsPublicInputs_NoManualHidden()
    {
        var id = await SeedMediaAsync(isPublic: false);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var html = await client.GetStringAsync($"/admin/medias/edit/{id}");

        var inputCount = Regex.Matches(html, @"name=""IsPublic""").Count;

        inputCount.Should().Be(2,
            because: "asp-for bool için MVC otomatik checkbox + 1 hidden üretir. " +
                     "Manuel hidden eklenmiş ise 3 input olur ve model binder bool'u her zaman false alır.");
    }

    /// <summary>
    /// Happy path: Edit POST IsPublic=true (checkbox işaretli) → DB'de IsPublic=true olmalı.
    /// Buggy state'te (manuel hidden ile) bu test fail eder.
    /// </summary>
    [Fact]
    public async Task Edit_PostWithIsPublicChecked_PersistsTrue()
    {
        var id = await SeedMediaAsync(isPublic: false);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var editUrl = $"/admin/medias/edit/{id}";
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, editUrl);

        // Browser submission of checkbox işaretli + asp-for otomatik hidden taklit edilir.
        // Browser tüm IsPublic name'li input'ları sırayla gönderir.
        // Buggy state: manuel hidden(false) + checkbox(true) + auto hidden(false) → 3 değer
        // Fix sonrası: checkbox(true) + auto hidden(false) → 2 değer
        var html = await client.GetStringAsync(editUrl);
        var allIsPublicValues = Regex.Matches(html,
                @"name=""IsPublic""[^>]*value=""([^""]+)""")
            .Select(m => m.Groups[1].Value)
            .ToList();
        // Checkbox işaretli kabul edilir — auto hidden checkbox'tan sonra render edilir
        // ama checkbox işaretliyse browser hem checkbox(true) hem auto hidden(false) gönderir.

        var formFields = new List<KeyValuePair<string, string>>
        {
            new("Id", id.ToString()),
            new("AltText", "test"),
        };
        foreach (var value in allIsPublicValues)
        {
            // Eğer checkbox(true) varsa checkbox işaretli kabul edip "true" gönderir
            formFields.Add(new("IsPublic", value));
        }
        // Checkbox'ı "işaretli" simüle et: eğer "true" değeri henüz yoksa ekle
        if (!allIsPublicValues.Contains("true"))
        {
            formFields.Add(new("IsPublic", "true"));
        }
        formFields.Add(new("__RequestVerificationToken", token));

        var response = await client.PostAsync(editUrl, new FormUrlEncodedContent(formFields));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            because: "Success path Details'a redirect eder.");

        var dbValue = await ReadIsPublicAsync(id);
        dbValue.Should().BeTrue(
            because: "İşaretli checkbox + form submit DB'ye true yazmalı. " +
                     "False kalıyorsa form binding bug var.");
    }

    /// <summary>
    /// Negative: checkbox işaretsiz → DB IsPublic=false olmalı (toggle off davranışı).
    /// </summary>
    [Fact]
    public async Task Edit_PostWithIsPublicUnchecked_PersistsFalse()
    {
        var id = await SeedMediaAsync(isPublic: true); // önceden public

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var editUrl = $"/admin/medias/edit/{id}";
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, editUrl);

        // Browser submission of checkbox İŞARETSİZ taklit: sadece auto hidden (false) gönderilir.
        // asp-for auto hidden checkbox'tan sonra render edilir; işaretsiz checkbox browser tarafından gönderilmez.
        var formFields = new List<KeyValuePair<string, string>>
        {
            new("Id", id.ToString()),
            new("AltText", "test"),
            new("IsPublic", "false"), // sadece auto hidden değeri
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync(editUrl, new FormUrlEncodedContent(formFields));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var dbValue = await ReadIsPublicAsync(id);
        dbValue.Should().BeFalse(
            because: "İşaretsiz checkbox = false; toggle off davranışı çalışmalı.");
    }
}
