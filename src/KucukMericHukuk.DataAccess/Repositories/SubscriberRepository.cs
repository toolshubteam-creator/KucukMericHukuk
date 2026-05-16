using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class SubscriberRepository : GenericRepository<Subscriber>, ISubscriberRepository
{
    public SubscriberRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<Subscriber>> GetAdminPagedAsync(
        string? keyword,
        SubscriberStatus? status,
        bool includeDeleted,
        int page,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbSet.IgnoreQueryFilters()
            : _dbSet.AsQueryable();

        if (status.HasValue)
        {
            var s = status.Value;
            query = query.Where(x => x.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(x => x.Email.Contains(k));
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(x => x.SubscribedAt >= start);
        }
        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);
            query = query.Where(x => x.SubscribedAt < endExclusive);
        }

        query = query.OrderByDescending(x => x.SubscribedAt);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Subscriber?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        // IgnoreQueryFilters: re-subscribe akışında soft-deleted kaydı da yakalamamız gerekebilir.
        return _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Email == normalized, ct);
    }

    public Task<Subscriber?> GetByTokenAsync(Guid token, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(s => s.UnsubscribeToken == token, ct);

    public Task<Subscriber?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
}
