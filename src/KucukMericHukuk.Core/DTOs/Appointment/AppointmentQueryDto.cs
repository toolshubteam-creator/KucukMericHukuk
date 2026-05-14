using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.Appointment;

public class AppointmentQueryDto
{
    public string? Keyword { get; set; }

    /// <summary>Durum filtresi — null ise tum durumlar. Repository dogrudan enum esitlik filtresi uygular.</summary>
    public AppointmentStatus? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>Tarih araligi filter — CreatedAt &gt;= StartDate (UTC). Inclusive.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Tarih araligi filter — CreatedAt &lt; EndDate + 1 day (UTC). Repository [start, end+1d) exclusive uygular.</summary>
    public DateTime? EndDate { get; set; }
}
