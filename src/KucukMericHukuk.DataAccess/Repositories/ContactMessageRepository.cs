using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class ContactMessageRepository : GenericRepository<ContactMessage>, IContactMessageRepository
{
    public ContactMessageRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<ContactMessage>> GetAdminPagedAsync(
        string? keyword,
        ContactMessageStatusFilter status,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbSet.IgnoreQueryFilters()
            : _dbSet.AsQueryable();

        query = status switch
        {
            ContactMessageStatusFilter.Unread   => query.Where(m => !m.IsRead),
            ContactMessageStatusFilter.Read     => query.Where(m => m.IsRead && !m.IsAnswered),
            ContactMessageStatusFilter.Answered => query.Where(m => m.IsAnswered),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(m =>
                m.Name.Contains(k) ||
                m.Email.Contains(k) ||
                m.Subject.Contains(k) ||
                m.Message.Contains(k));
        }

        query = query.OrderByDescending(m => m.CreatedAt);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<ContactMessage?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
}
