using System.Net;
using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.IntegrationTests.Appointments;

/// <summary>
/// Faz 6.22 — Randevu modulu: public form POST + admin liste + durum aksiyonlari.
/// ContactMessagesAdminTests sablon alindi.
/// </summary>
public class AppointmentsAdminTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AppointmentsAdminTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> SeedAppointmentAsync(AppointmentStatus status = AppointmentStatus.Pending)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Set<Appointment>().RemoveRange(db.Set<Appointment>().IgnoreQueryFilters().ToList());
        await db.SaveChangesAsync();

        var appt = new Appointment
        {
            Name = "Test Muvekkil",
            Email = "muvekkil@example.com",
            Phone = "5551112233",
            Subject = "Test Konu",
            PreferredDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            PreferredTimeNote = "Ogleden sonra",
            Notes = "Test aciklama",
            KvkkConsent = true,
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
        db.Set<Appointment>().Add(appt);
        await db.SaveChangesAsync();
        return appt.Id;
    }

    [Fact]
    public async Task PostPublicForm_ValidData_PersistsAppointmentWithPendingStatus()
    {
        // Onceki test verilerini temizle
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<Appointment>().RemoveRange(db.Set<Appointment>().IgnoreQueryFilters().ToList());
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, "/tr-TR/Appointment");

        var preferredDate = DateTime.UtcNow.Date.AddDays(10).ToString("yyyy-MM-dd");
        var formData = new List<KeyValuePair<string, string>>
        {
            new("Name", "Yeni Talep"),
            new("Email", "yeni@example.com"),
            new("Phone", "5559998877"),
            new("Subject", "Bosanma danismanligi"),
            new("PreferredDate", preferredDate),
            new("PreferredTimeNote", "14:00 civari"),
            new("Notes", "Detayli gorusmek istiyorum."),
            new("KvkkConsent", "true"),
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync("/tr-TR/Appointment/Submit",
            new FormUrlEncodedContent(formData));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("Appointment/ThankYou");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appt = await verifyDb.Set<Appointment>().FirstAsync(a => a.Email == "yeni@example.com");
        appt.Name.Should().Be("Yeni Talep");
        appt.Phone.Should().Be("5559998877");
        appt.Status.Should().Be(AppointmentStatus.Pending);
        appt.KvkkConsent.Should().BeTrue();
    }

    [Fact]
    public async Task GetAdminIndex_Authorized_ReturnsOkWithAppointment()
    {
        await SeedAppointmentAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await TestHelpers.LoginAsync(client);

        var response = await client.GetAsync("/admin/appointments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Test Muvekkil");
        html.Should().Contain("Randevu");
    }

    [Fact]
    public async Task PostConfirm_Authorized_SetsConfirmedStatus()
    {
        var id = await SeedAppointmentAsync(AppointmentStatus.Pending);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var detailsUrl = $"/admin/appointments/details/{id}";
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, detailsUrl);

        var formData = new List<KeyValuePair<string, string>>
        {
            new("adminNote", "Saat 14:00 uygundur."),
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync($"/admin/appointments/confirm/{id}",
            new FormUrlEncodedContent(formData));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain($"/admin/appointments/details/{id}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appt = await db.Set<Appointment>().FirstAsync(a => a.Id == id);
        appt.Status.Should().Be(AppointmentStatus.Confirmed);
        appt.AdminNote.Should().Be("Saat 14:00 uygundur.");
    }

    [Fact]
    public async Task PostReject_Authorized_SetsRejectedStatus()
    {
        var id = await SeedAppointmentAsync(AppointmentStatus.Pending);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var detailsUrl = $"/admin/appointments/details/{id}";
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, detailsUrl);

        var formData = new List<KeyValuePair<string, string>>
        {
            new("adminNote", "Belirtilen tarihte uygun degiliz."),
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync($"/admin/appointments/reject/{id}",
            new FormUrlEncodedContent(formData));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appt = await db.Set<Appointment>().FirstAsync(a => a.Id == id);
        appt.Status.Should().Be(AppointmentStatus.Rejected);
    }

    [Fact]
    public async Task PostDelete_Authorized_SoftDeletesAppointment()
    {
        var id = await SeedAppointmentAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await TestHelpers.LoginAsync(client);

        var detailsUrl = $"/admin/appointments/details/{id}";
        var token = await TestHelpers.GetAntiForgeryTokenAsync(client, detailsUrl);

        var formData = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
        };

        var response = await client.PostAsync($"/admin/appointments/delete/{id}",
            new FormUrlEncodedContent(formData));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appt = await db.Set<Appointment>().IgnoreQueryFilters().FirstAsync(a => a.Id == id);
        appt.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAdminIndex_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/admin/appointments");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Contain("/admin/account/login");
    }
}
