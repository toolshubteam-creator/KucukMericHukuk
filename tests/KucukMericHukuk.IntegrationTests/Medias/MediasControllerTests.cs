using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace KucukMericHukuk.IntegrationTests.Medias;

public class MediasControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public MediasControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private static byte[] CreateJpegBytes(int width = 200, int height = 200)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.SkyBlue);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    [Fact]
    public async Task Index_AnonymousUser_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/admin/medias");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/account/login");
    }

    [Fact]
    public async Task Index_Authenticated_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/medias");

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Medya");
    }

    [Fact]
    public async Task PickerList_Authenticated_ReturnsJson()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/medias/picker-list?page=1&pageSize=24");

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"totalCount\"");
    }

    [Fact]
    public async Task Upload_Authenticated_ValidJpeg_ReturnsSuccess()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await TestHelpers.LoginAsync(client);

        // Galeri sayfasından AntiForgery token al (upload zone içinde render ediliyor)
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/admin/medias");

        var jpegBytes = CreateJpegBytes(150, 100);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test-upload.jpg");
        content.Add(new StringContent(token), "__RequestVerificationToken");

        var response = await client.PostAsync("/admin/medias/upload", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"success\":true");

        // DB'de MediaFile oluştu mu
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var any = await ctx.Set<MediaFile>().AnyAsync();
        any.Should().BeTrue();
    }
}
