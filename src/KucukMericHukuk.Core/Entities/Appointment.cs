using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.Entities;

/// <summary>
/// Public randevu talebi (form-tabanli). Takvim/slot yonetimi YOK — tarih/saat muvekkil tercihi olarak girilir.
/// ContactMessage modeli sablon alindi; reply tablosu YOK — durum degisimi tek e-posta tetikler (Faz 6.22).
/// </summary>
public class Appointment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Randevu talebinde telefon ZORUNLU (ContactMessage'da opsiyoneldi).</summary>
    public string Phone { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    /// <summary>Muvekkilin tercih ettigi tarih (zorunlu).</summary>
    public DateOnly PreferredDate { get; set; }

    /// <summary>Tercih edilen saat — serbest metin not (takvim/slot YOK).</summary>
    public string? PreferredTimeNote { get; set; }

    /// <summary>Muvekkilin ek aciklamasi (opsiyonel).</summary>
    public string? Notes { get; set; }

    public bool KvkkConsent { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    /// <summary>Admin'in onay/red gerekcesi — Confirm/Reject e-postasina opsiyonel olarak eklenir.</summary>
    public string? AdminNote { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
