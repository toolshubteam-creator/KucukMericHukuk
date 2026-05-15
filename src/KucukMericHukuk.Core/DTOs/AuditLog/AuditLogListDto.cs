using KucukMericHukuk.Core.Enums;

namespace KucukMericHukuk.Core.DTOs.AuditLog;

public class AuditLogListDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditActionType Action { get; set; }
}
