using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.AuditLog;

public class AuditLogQueryDto
{
    /// <summary>EntityName veya UserName içinde substring araması.</summary>
    public string? Keyword { get; set; }

    public int? UserId { get; set; }
    public string? EntityName { get; set; }
    public AuditActionType? Action { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;

    /// <summary>Tarih araligi filter — CreatedAt &gt;= StartDate (UTC). Inclusive.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Tarih araligi filter — CreatedAt &lt; EndDate + 1 day (UTC). Repository [start, end+1d) exclusive uygular.</summary>
    public DateTime? EndDate { get; set; }
}
