using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.Entities;

/// <summary>
/// Audit kaydı — admin CRUD işlemleri için otomatik üretilir (AuditSaveChangesInterceptor).
/// BaseEntity DEĞİL: soft-delete YOK (audit log'u silinmemeli), Id Guid.
/// Identity entity'leri, AuditLog kendisi ve public form girişleri (ContactMessage/Appointment/Subscriber)
/// interceptor ignore listesinde — bunlar audit'lenmez.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>İşlemi yapan admin kullanıcısı. Sistem işlemi (background job vb.) varsa null.</summary>
    public int? UserId { get; set; }

    /// <summary>Snapshot — kullanıcı sonradan silinse de okunabilir.</summary>
    public string? UserName { get; set; }

    /// <summary>Etkilenen entity tip adı (örn. "Article", "Service"). Index'lenir.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Etkilenen entity primary key — hem int hem Guid'i string olarak tutmak için.</summary>
    public string EntityId { get; set; } = string.Empty;

    public AuditActionType Action { get; set; }

    /// <summary>
    /// İşlem detayı JSON:
    /// - Created: tüm yeni değerler snapshot
    /// - Modified: SADECE değişen alanlar — { field: { old, new } } delta
    /// - Deleted: silinen entity'nin son hali snapshot
    /// - Restored: null veya minimal mesaj
    /// </summary>
    public string? ChangesJson { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
