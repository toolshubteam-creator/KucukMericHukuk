using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<Appointment>> GetAdminPagedAsync(
        string? keyword,
        AppointmentStatus? status,
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

        // Durum filtresi: DOGRUDAN enum esitlik. ContactMessage bool-turevli filtre kullaniyordu;
        // burada Status tek bir enum kolonu oldugu icin esitlik karsilastirmasi yeterli.
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(a =>
                a.Name.Contains(k) ||
                a.Email.Contains(k) ||
                a.Phone.Contains(k) ||
                a.Subject.Contains(k));
        }

        // Tarih araligi: [startDate, endDate+1day) exclusive — CreatedAt (talep tarihi) uzerinden
        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            query = query.Where(a => a.CreatedAt >= start);
        }
        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < endExclusive);
        }

        query = query.OrderByDescending(a => a.CreatedAt);

        return await query.AsNoTracking().ToPagedListAsync(page, pageSize, ct);
    }

    public Task<Appointment?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
}
