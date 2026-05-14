using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    /// <summary>
    /// Admin liste sorgusu — keyword + tarih araligi + durum filtresi.
    /// Durum filtresi DOGRUDAN enum esitlik uygular (ContactMessage'daki bool-turevli filtreden farkli).
    /// </summary>
    Task<PagedResult<Appointment>> GetAdminPagedAsync(
        string? keyword,
        AppointmentStatus? status,
        bool includeDeleted,
        int page,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);

    /// <summary>Soft-deleted kayitlar dahil tek randevu — admin Details + Restore senaryolari.</summary>
    Task<Appointment?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
}
