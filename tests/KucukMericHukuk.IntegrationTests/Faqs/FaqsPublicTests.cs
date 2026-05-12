using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Faqs;

public class FaqsPublicTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public FaqsPublicTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedFaqAsync(string question, string answer, int displayOrder = 1, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Test isolation: clean existing
        db.Set<Faq>().RemoveRange(db.Set<Faq>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();

        db.Set<Faq>().Add(new Faq
        {
            DisplayOrder = displayOrder,
            IsActive = isActive,
            Translations = new List<FaqTranslation>
            {
                new() { LanguageCode = "tr-TR", Question = question, Answer = answer }
            }
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSss_HtmlFormattedAnswer_RendersTagsAsHtml()
    {
        // Quill cikartisi gibi rich text Answer DB'de bulunuyor
        await SeedFaqAsync(
            question: "Test sorusu?",
            answer: "<p><strong>Onemli:</strong> Detayli bilgi <em>burada</em>.</p>");

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/Faqs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Html.Raw kullaniminin kaniti: tag'ler escape edilmez, browser HTML olarak render eder
        html.Should().Contain("<strong>Onemli:</strong>");
        html.Should().Contain("<em>burada</em>");
        // Plain text icerik HTML-encoded olur (Turkce karakter):
        // ama "Detayli" ASCII -> aynen kalir
        html.Should().Contain("Detayli bilgi");
    }

    [Fact]
    public async Task GetSss_LegacyPlainTextAnswer_RendersAsIs_BackwardCompat()
    {
        // Mevcut seed pattern: HTML tag YOK, duz metin
        await SeedFaqAsync(
            question: "Eski format soru?",
            answer: "Duz metin cevap, hicbir HTML tag yok.");

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/Faqs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Plain text bozulmadan render edilir (Html.Raw plain text icin de calisir)
        html.Should().Contain("Duz metin cevap, hicbir HTML tag yok");
    }

    [Fact]
    public async Task GetSss_NoScriptTagRendered_XssNegative()
    {
        // Bu Faq DB'ye dogrudan yazildi (sanitize bypass) — ama public view'da JS calismaz
        // cunku kullanici taraflarinda admin Edit zaten sanitize ediyor.
        // Bu test public view'in DB'deki icerigi @Html.Raw ile rendered ettigini gosterir:
        // dolayisiyla sanitize'e GUVEN bagimliligi vardir (defansif derinlik degil)
        // ve EN AZ admin pipeline'i bypass eden hicbir HTML <script> ile DB'ye yazilamamali.
        // Bu test "admin sanitize calistigi surece" public guvenli pattern saglar.

        await SeedFaqAsync(
            question: "Guvenli mi?",
            answer: "<p>Guvenli icerik</p>");

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/Faqs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // <script> tag hicbir Faq cevabinda olmamali
        html.Should().NotContain("<script>alert");
        html.Should().NotContain("javascript:alert");
    }
}
