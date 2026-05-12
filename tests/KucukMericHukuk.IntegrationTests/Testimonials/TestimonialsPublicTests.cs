using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Testimonials;

public class TestimonialsPublicTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TestimonialsPublicTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedAsync(params (string Initials, string Content, bool IsActive)[] data)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clean existing testimonials for test isolation
        db.Set<Testimonial>().RemoveRange(db.Set<Testimonial>().ToList());
        await db.SaveChangesAsync();

        var order = 1;
        foreach (var (initials, content, isActive) in data)
        {
            db.Set<Testimonial>().Add(new Testimonial
            {
                AuthorInitials = initials,
                AuthorRole = "Müvekkil",
                Rating = 5,
                IsActive = isActive,
                IsFeatured = false,
                DisplayOrder = order++,
                Translations = new List<TestimonialTranslation>
                {
                    new() { LanguageCode = "tr-TR", Content = content }
                }
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetReferanslar_Returns200WithTestimonialCards()
    {
        await SeedAsync(
            ("M.A.", "Cok profesyonel hizmet aldim.", true),
            ("E.Y.", "Detayli bilgilendirme icin tesekkurler.", true));

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/Referanslar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        // ASCII-safe substring (Turkce karakter HTML-encoded olabilir)
        html.Should().Contain("Cok profesyonel hizmet");
        html.Should().Contain("M.A.");
    }

    [Fact]
    public async Task GetReferanslar_EmptyDb_ShowsEmptyState()
    {
        await SeedAsync(); // hiç veri yok

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/Referanslar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        // Empty state mesajı veya boş grid render edilir — view error vermez
        html.Should().NotContain("testimonial-card__quote");
    }

    [Fact]
    public async Task GetReferanslar_InactiveTestimonial_NotShown()
    {
        await SeedAsync(
            ("A.B.", "Aktif yorum icerigi.", true),
            ("C.D.", "Pasif yorum icerigi.", false));

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR/Referanslar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Aktif yorum icerigi");
        html.Should().NotContain("Pasif yorum icerigi");
    }
}
