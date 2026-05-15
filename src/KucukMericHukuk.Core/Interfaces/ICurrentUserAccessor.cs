namespace KucukMericHukuk.Core.Interfaces;

/// <summary>
/// Mevcut request'in kullanıcı + IP bilgisini katman-bağımsız şekilde sağlar.
/// Web katmanında <c>IHttpContextAccessor</c> üzerinden implement edilir; arka plan görevleri,
/// migration, test gibi HTTP'siz senaryolarda null döner.
/// DataAccess (AuditSaveChangesInterceptor) bu soyutlama üzerinden tüketir — temiz katman.
/// </summary>
public interface ICurrentUserAccessor
{
    int? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
}
