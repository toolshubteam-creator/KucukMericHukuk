using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Appointment;
using KucukMericHukuk.Core.DTOs.Common;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IAppointmentService
{
    // Public
    Task<Result<int>> SaveAsync(AppointmentFormDto form, CancellationToken ct = default);

    // Admin
    Task<Result<PagedResult<AppointmentListDto>>> GetPagedAsync(
        AppointmentQueryDto query, CancellationToken ct = default);

    Task<Result<AppointmentAdminDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Durum -> Confirmed. Muvekkile onay e-postasi gonderir (adminNote opsiyonel gerekce).</summary>
    Task<Result> ConfirmAsync(int id, string? adminNote, CancellationToken ct = default);

    /// <summary>Durum -> Rejected. Muvekkile red e-postasi gonderir (adminNote opsiyonel gerekce).</summary>
    Task<Result> RejectAsync(int id, string? adminNote, CancellationToken ct = default);

    /// <summary>Durum -> Completed. E-posta GONDERILMEZ.</summary>
    Task<Result> CompleteAsync(int id, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    Task<Result> RestoreAsync(int id, CancellationToken ct = default);
}
