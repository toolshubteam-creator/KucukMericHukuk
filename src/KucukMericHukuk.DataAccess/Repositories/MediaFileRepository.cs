using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class MediaFileRepository : GenericRepository<MediaFile>, IMediaFileRepository
{
    public MediaFileRepository(AppDbContext context) : base(context) { }

    public Task<MediaFile?> GetByHashAsync(string sha256, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(x => x.Sha256 == sha256, ct);

    public Task<MediaFile?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<PagedResult<MediaFile>> GetAdminPagedAsync(
        string? keyword, int page, int pageSize, bool includeDeleted, CancellationToken ct = default)
    {
        var query = includeDeleted ? _dbSet.IgnoreQueryFilters() : _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(m =>
                m.OriginalFileName.Contains(keyword) ||
                (m.AltText != null && m.AltText.Contains(keyword)));
        }

        query = query.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }
}
