using System.Net;
using FluentAssertions;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KucukMericHukuk.IntegrationTests.Routing;

public class PublicRouteLocalizationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public PublicRouteLocalizationTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/tr-TR/Contact", "/tr-TR/iletisim")]
    [InlineData("/tr-TR/Contact/Index", "/tr-TR/iletisim")]
    [InlineData("/tr-TR/Contact/ThankYou", "/tr-TR/iletisim/tesekkurler")]
    [InlineData("/tr-TR/Appointment", "/tr-TR/randevu")]
    [InlineData("/tr-TR/Appointment/Index", "/tr-TR/randevu")]
    [InlineData("/tr-TR/Appointment/ThankYou", "/tr-TR/randevu/tesekkurler")]
    [InlineData("/tr-TR/Articles", "/tr-TR/makaleler")]
    [InlineData("/tr-TR/Articles/eski-slug", "/tr-TR/makaleler/eski-slug")]
    [InlineData("/tr-TR/Services", "/tr-TR/hizmetler")]
    [InlineData("/tr-TR/Services/ceza-hukuku", "/tr-TR/hizmetler/ceza-hukuku")]
    [InlineData("/tr-TR/Attorneys", "/tr-TR/avukatlar")]
    [InlineData("/tr-TR/Attorneys/av-demo", "/tr-TR/avukatlar/av-demo")]
    [InlineData("/tr-TR/Pages/hakkimizda", "/tr-TR/sayfalar/hakkimizda")]
    [InlineData("/tr-TR/Faqs", "/tr-TR/sss")]
    [InlineData("/tr-TR/Galeri", "/tr-TR/galeri")]
    [InlineData("/tr-TR/Referanslar", "/tr-TR/referanslar")]
    public async Task Legacy_public_get_routes_redirect_permanently_to_turkish_routes(
        string oldPath,
        string expectedPath)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(oldPath);

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be(expectedPath);
    }

    [Theory]
    [InlineData("/tr-TR/iletisim")]
    [InlineData("/tr-TR/randevu")]
    [InlineData("/tr-TR/makaleler")]
    [InlineData("/tr-TR/hizmetler")]
    [InlineData("/tr-TR/avukatlar")]
    [InlineData("/tr-TR/sss")]
    [InlineData("/tr-TR/galeri")]
    [InlineData("/tr-TR/referanslar")]
    [InlineData("/en-US/contact")]
    [InlineData("/en-US/appointment")]
    [InlineData("/en-US/articles")]
    [InlineData("/en-US/services")]
    [InlineData("/en-US/attorneys")]
    [InlineData("/en-US/faq")]
    [InlineData("/en-US/gallery")]
    [InlineData("/en-US/testimonials")]
    public async Task Canonical_public_get_routes_are_routable(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        response.StatusCode.Should().NotBe(HttpStatusCode.MovedPermanently);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
