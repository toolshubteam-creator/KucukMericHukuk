using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces.Repositories;

public interface IMediaFileRepository : IGenericRepository<MediaFile>
{
    Task<MediaFile?> GetByHashAsync(string sha256, CancellationToken ct = default);
    Task<MediaFile?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);
    Task<PagedResult<MediaFile>> GetAdminPagedAsync(string? keyword, int page, int pageSize, bool includeDeleted, CancellationToken ct = default);

    /// <summary>Public /Galeri sayfası için IsPublic=true + image content-type, CreatedAt DESC.</summary>
    Task<PagedResult<MediaFile>> GetPublicPagedAsync(int page, int pageSize, CancellationToken ct = default);
}
