using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

/// <summary>
/// Manuel redirect repo (Faz 7.4.1). Middleware (7.4.2) sıcak yolda
/// `GetByFromPathAsync` çağırır — sadece aktif kayıt döner.
/// </summary>
public class RedirectRepository : IRedirectRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Redirect> _dbSet;

    public RedirectRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Redirects;
    }

    public Task<Redirect?> GetByFromPathAsync(string fromPath, CancellationToken ct = default)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(
            r => r.IsActive && r.FromPath == fromPath, ct);

    public Task<Redirect?> GetByFromPathAnyAsync(string fromPath, CancellationToken ct = default)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(r => r.FromPath == fromPath, ct);

    /// <summary>
    /// Atomik ExecuteUpdate: HitCount++ + LastHitAt=now. AuditSaveChangesInterceptor
    /// görmez (raw SQL UPDATE, ChangeTracker dışı) → admin "Aktivite Logu" gürültü
    /// yaratmaz. NotFoundLog.RecordHitAsync pattern.
    /// </summary>
    public async Task<bool> RecordHitAsync(int id, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _dbSet
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.HitCount, r => r.HitCount + 1)
                .SetProperty(r => r.LastHitAt, _ => now),
                ct);
        return rows > 0;
    }

    public async Task<PagedResult<Redirect>> GetAdminPagedAsync(
        RedirectQueryDto query, CancellationToken ct = default)
    {
        var q = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.Trim();
            q = q.Where(r => r.FromPath.Contains(k) || r.ToPath.Contains(k));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(r => r.IsActive == query.IsActive.Value);
        }

        q = q.OrderByDescending(r => r.CreatedAt);

        return await q.AsNoTracking().ToPagedListAsync(query.Page, query.PageSize, ct);
    }

    public Task<Redirect?> GetByIdAsync(int id, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> ExistsFromPathAsync(string fromPath, int? excludeId = null, CancellationToken ct = default)
        => excludeId.HasValue
            ? _dbSet.AnyAsync(r => r.FromPath == fromPath && r.Id != excludeId.Value, ct)
            : _dbSet.AnyAsync(r => r.FromPath == fromPath, ct);

    public Task AddAsync(Redirect entity, CancellationToken ct = default)
        => _dbSet.AddAsync(entity, ct).AsTask();

    public void Update(Redirect entity) => _dbSet.Update(entity);

    public void Delete(Redirect entity) => _dbSet.Remove(entity);
}
