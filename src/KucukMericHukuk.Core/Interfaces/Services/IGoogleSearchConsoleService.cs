using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Google;

namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IGoogleSearchConsoleService
{
    Task<Result<GoogleSearchConsoleDashboardWidgetDto>> GetDashboardWidgetAsync(CancellationToken ct = default);
}
