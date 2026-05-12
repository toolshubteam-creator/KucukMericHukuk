using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Contact;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IContactMessageService
{
    // Public
    Task<Result<int>> SaveAsync(ContactFormDto form, CancellationToken ct = default);

    // Admin
    Task<Result<PagedResult<ContactMessageListDto>>> GetPagedAsync(
        ContactMessageQueryDto query, CancellationToken ct = default);

    Task<Result<ContactMessageAdminDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Details GET tarafından çağrılır; idempotent (IsRead zaten true ise no-op).</summary>
    Task<Result> MarkAsReadAsync(int id, CancellationToken ct = default);

    /// <summary>Manuel toggle — okunmuş/okunmamış arası.</summary>
    Task<Result> ToggleReadAsync(int id, CancellationToken ct = default);

    /// <summary>Manuel toggle — yanıtlanmış/yanıtlanmamış arası.</summary>
    Task<Result> ToggleAnsweredAsync(int id, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(int id, CancellationToken ct = default);

    // ───────────── Faz 6.7 ─────────────

    /// <summary>
    /// Admin'in mesaja yanıt göndermesi. Reply kaydı + IsAnswered=true + IsRead=true (idempotent) + email (SMTP yapılandırılmışsa).
    /// Email gönderim hatası YUTULUR (SmtpEmailSender log only) — reply DB'de korunur.
    /// </summary>
    Task<Result<int>> ReplyAsync(ContactMessageReplyInputDto input, int? sentByUserId, CancellationToken ct = default);

    /// <summary>Admin Dashboard widget: okunmamış mesaj sayısı.</summary>
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);

    /// <summary>Admin Dashboard widget: son N mesaj (CreatedAt DESC).</summary>
    Task<IReadOnlyList<ContactMessageListDto>> GetRecentAsync(int count, CancellationToken ct = default);
}
