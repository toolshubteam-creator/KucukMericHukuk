using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Google;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IGoogleApiClient
{
    Task<Result<GoogleApiClientStatusDto>> GetStatusAsync(CancellationToken ct = default);

    Task<Result<string>> GetAccessTokenAsync(
        IReadOnlyCollection<string> scopes,
        CancellationToken ct = default);
}
