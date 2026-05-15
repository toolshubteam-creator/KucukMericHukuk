using KucukMericHukuk.Core.DTOs.AuditLog;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

/// <summary>
/// AuditLog repo — readonly. AuditSaveChangesInterceptor üretir, repo yalnız okur.
/// IGenericRepository implement etmez (BaseEntity türü değil, Add/Update/Delete semantic'i de istenmez).
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<AuditLog> _dbSet;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.AuditLogs;
    }

    public async Task<PagedResult<AuditLog>> GetAdminPagedAsync(
        AuditLogQueryDto query, CancellationToken ct = default)
    {
        var q = _dbSet.AsQueryable();

        if (query.UserId.HasValue)
        {
            q = q.Where(a => a.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            q = q.Where(a => a.EntityName == query.EntityName);
        }

        if (query.Action.HasValue)
        {
            q = q.Where(a => a.Action == query.Action.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            q = q.Where(a =>
                a.EntityName.Contains(k) ||
                (a.UserName != null && a.UserName.Contains(k)) ||
                a.EntityId.Contains(k));
        }

        if (query.StartDate.HasValue)
        {
            var start = query.StartDate.Value.Date;
            q = q.Where(a => a.CreatedAt >= start);
        }
        if (query.EndDate.HasValue)
        {
            var endExclusive = query.EndDate.Value.Date.AddDays(1);
            q = q.Where(a => a.CreatedAt < endExclusive);
        }

        q = q.OrderByDescending(a => a.CreatedAt);

        return await q.AsNoTracking().ToPagedListAsync(query.Page, query.PageSize, ct);
    }

    public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
}
