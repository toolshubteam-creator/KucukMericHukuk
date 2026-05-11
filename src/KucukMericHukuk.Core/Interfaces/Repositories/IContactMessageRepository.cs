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
        CancellationToken ct = default);

    Task<ContactMessage?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
}
