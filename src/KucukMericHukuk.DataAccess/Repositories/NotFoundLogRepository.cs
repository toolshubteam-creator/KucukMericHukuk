using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.NotFoundLog;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

/// <summary>
/// 404 hit aggregate repo (Faz 7.3.1). `IGenericRepository` implement etmez
/// (NotFoundLog BaseEntity değil, soft-delete yok). Yazma yalnız
/// `RecordHitAsync` üzerinden — race-safe upsert.
/// </summary>
public class NotFoundLogRepository : INotFoundLogRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<NotFoundLog> _dbSet;

    public NotFoundLogRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.NotFoundLogs;
    }

    /// <summary>
    /// Race-safe upsert akışı:
    ///   1) `ExecuteUpdateAsync WHERE Url=@u` — varsa HitCount++/LastSeenAt set; affected==1 → bitir
    ///   2) affected==0 → yeni satır Add + SaveChanges
    ///   3) Concurrent başka request araya girip aynı URL'i INSERT etmişse DB unique
    ///      constraint `DbUpdateException` fırlatır → entry'yi detach et + retry step 1
    /// Adım 1 ve 3 atomic SQL UPDATE — 2 paralel request aynı URL'e gelirse de hep tek satır.
    /// </summary>
    public async Task<bool> RecordHitAsync(
        string url,
        string? referer,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var affected = await _dbSet
            .Where(n => n.Url == url)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.HitCount, n => n.HitCount + 1)
                .SetProperty(n => n.LastSeenAt, _ => now),
                ct);

        if (affected > 0) return false;

        var entity = new NotFoundLog
        {
            Id = Guid.NewGuid(),
            Url = url,
            Referer = referer,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            HitCount = 1,
            FirstSeenAt = now,
            LastSeenAt = now
        };

        _dbSet.Add(entity);
        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Concurrent request araya girdi — entry'yi context'ten çıkar, retry-update.
            _context.Entry(entity).State = EntityState.Detached;

            await _dbSet
                .Where(n => n.Url == url)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.HitCount, n => n.HitCount + 1)
                    .SetProperty(n => n.LastSeenAt, _ => now),
                    ct);

            return false;
        }
    }

    public async Task<PagedResult<NotFoundLog>> GetAdminPagedAsync(
        NotFoundLogQueryDto query, CancellationToken ct = default)
    {
        var q = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            q = q.Where(n => n.Url.Contains(k));
        }

        if (query.StartDate.HasValue)
        {
            var start = query.StartDate.Value.Date;
            q = q.Where(n => n.LastSeenAt >= start);
        }
        if (query.EndDate.HasValue)
        {
            var endExclusive = query.EndDate.Value.Date.AddDays(1);
            q = q.Where(n => n.LastSeenAt < endExclusive);
        }

        q = query.Sort == "recent"
            ? q.OrderByDescending(n => n.LastSeenAt).ThenByDescending(n => n.HitCount)
            : q.OrderByDescending(n => n.HitCount).ThenByDescending(n => n.LastSeenAt);

        return await q.AsNoTracking().ToPagedListAsync(query.Page, query.PageSize, ct);
    }

    public Task<NotFoundLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<bool> PurgeAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await _dbSet.Where(n => n.Id == id).ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public Task<int> PurgeAllAsync(CancellationToken ct = default)
        => _dbSet.ExecuteDeleteAsync(ct);
}
