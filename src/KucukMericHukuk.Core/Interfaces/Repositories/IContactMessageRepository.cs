using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IContactMessageRepository : IGenericRepository<ContactMessage>
{
    Task<PagedResult<ContactMessage>> GetAdminPagedAsync(
        string? keyword,
        ContactMessageStatusFilter status,
        bool includeDeleted,
        int page,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);

    Task<ContactMessage?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);

    /// <summary>Details için Replies + SentByUser dahil — admin sayfası kullanır.</summary>
    Task<ContactMessage?> GetByIdWithRepliesAsync(int id, CancellationToken ct = default);

    /// <summary>Dashboard widget: okunmamış (IsRead=false) mesaj sayısı (soft-delete hariç).</summary>
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);

    /// <summary>Dashboard widget: son N mesaj — CreatedAt DESC, soft-delete hariç.</summary>
    Task<IReadOnlyList<ContactMessage>> GetRecentAsync(int count, CancellationToken ct = default);
}
