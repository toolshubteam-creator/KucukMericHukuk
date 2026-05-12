using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Home;

public class HomePageTestimonialsTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public HomePageTestimonialsTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedAsync(params (string Initials, string Content, bool IsActive, bool IsFeatured)[] data)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<Testimonial>().RemoveRange(db.Set<Testimonial>().ToList());
        await db.SaveChangesAsync();

        var order = 1;
        foreach (var (initials, content, isActive, isFeatured) in data)
        {
            db.Set<Testimonial>().Add(new Testimonial
            {
                AuthorInitials = initials,
                AuthorRole = "Müvekkil",
                Rating = 5,
                IsActive = isActive,
                IsFeatured = isFeatured,
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
    public async Task GetHome_OnlyFeaturedTestimonials_AreRendered()
    {
        await SeedAsync(
            ("M.A.", "Cok profesyonel hizmet aldim.", true, true),     // featured
            ("E.Y.", "Detayli bilgilendirme, tesekkurler.", true, true), // featured
            ("K.D.", "Kalitatif hizmet sundular gercekten.", true, false)); // NOT featured

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Featured olanlar Ana Sayfa'da görünür (ASCII-only substring)
        html.Should().Contain("Cok profesyonel hizmet");
        html.Should().Contain("Detayli bilgilendirme");
        html.Should().Contain("M.A.");
        html.Should().Contain("E.Y.");

        // Featured olmayan görünmez
        html.Should().NotContain("Kalitatif hizmet sundular");

        // Section başlığı + CTA butonu (ASCII-safe assertion'lar)
        html.Should().Contain("home-testimonials-heading");
        html.Should().Contain("/tr-TR/Referanslar"); // "Tüm yorumları gör" link href
    }

    [Fact]
    public async Task GetHome_NoFeaturedTestimonials_SectionNotRendered()
    {
        await SeedAsync(
            ("X.Y.", "Aktif ama ferimli degil.", true, false)); // hiç featured yok

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/tr-TR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Section render edilmemeli — early return partial içinde
        html.Should().NotContain("home-testimonials-heading");
        html.Should().NotContain("Aktif ama ferimli degil");
    }
}
