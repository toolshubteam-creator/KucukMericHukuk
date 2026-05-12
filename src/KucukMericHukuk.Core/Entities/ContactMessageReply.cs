using KucukMericHukuk.Core.Entities.Identity;

namespace KucukMericHukuk.Core.Entities;

/// <summary>
/// Admin'in iletişim mesajına gönderdiği yanıtın history kaydı.
/// Soft-delete YOK (parent ContactMessage soft-deleted olsa da reply history korunur — KVKK retention politikasıyla yönetilir).
/// </summary>
public class ContactMessageReply
{
    public int Id { get; set; }

    public int ContactMessageId { get; set; }
    public ContactMessage ContactMessage { get; set; } = null!;

    /// <summary>Plain text yanıt gövdesi (HTML editor YOK — KISS, ileride Quill DEFERRED).</summary>
    public string Body { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>Yanıtı gönderen admin kullanıcısı. User silinince SetNull (history korunur).</summary>
    public int? SentByUserId { get; set; }
    public ApplicationUser? SentByUser { get; set; }
}
