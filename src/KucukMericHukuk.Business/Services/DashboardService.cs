using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Dashboard;
using KucukMericHukuk.Core.DTOs.Google;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;

namespace KucukMericHukuk.Business.Services;

/// <summary>
/// Admin Dashboard widget'larının veri kaynağı (Faz 6.21). ContactMessageService'in
/// dashboard widget metotlarıyla aynı rol — IUnitOfWork üzerinden salt-okuma, mutasyon yok.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IGoogleAnalyticsService _googleAnalyticsService;
    private readonly IGoogleSearchConsoleService _googleSearchConsoleService;

    public DashboardService(
        IUnitOfWork uow,
        IMapper mapper,
        IGoogleAnalyticsService googleAnalyticsService,
        IGoogleSearchConsoleService googleSearchConsoleService)
    {
        _uow = uow;
        _mapper = mapper;
        _googleAnalyticsService = googleAnalyticsService;
        _googleSearchConsoleService = googleSearchConsoleService;
    }

    public async Task<RecentArticlesWidgetDto> GetRecentArticlesAsync(
        string languageCode, int count, CancellationToken ct = default)
    {
        var recent = await _uow.Articles.GetRecentAsync(languageCode, count, ct);
        var total = await _uow.Articles.CountAsync(ct: ct);

        return new RecentArticlesWidgetDto
        {
            TotalCount = total,
            RecentArticles = _mapper.Map<List<ArticleListDto>>(recent),
        };
    }

    public async Task<SiteSummaryWidgetDto> GetSiteSummaryAsync(CancellationToken ct = default)
    {
        // CountAsync (predicate'siz) soft-delete query filter'ından geçer → aktif kayıt sayısı.
        return new SiteSummaryWidgetDto
        {
            ArticleCount = await _uow.Articles.CountAsync(ct: ct),
            ServiceCount = await _uow.Services.CountAsync(ct: ct),
            AttorneyCount = await _uow.Attorneys.CountAsync(ct: ct),
            PageCount = await _uow.Pages.CountAsync(ct: ct),
            MediaCount = await _uow.MediaFiles.CountAsync(ct: ct),
            FaqCount = await _uow.Faqs.CountAsync(ct: ct),
            TestimonialCount = await _uow.Testimonials.CountAsync(ct: ct),
        };
    }

    public async Task<GoogleAnalyticsDashboardWidgetDto> GetGoogleAnalyticsWidgetAsync(
        CancellationToken ct = default)
    {
        var result = await _googleAnalyticsService.GetDashboardWidgetAsync(ct);
        return result.IsSuccess
            ? result.Value
            : new GoogleAnalyticsDashboardWidgetDto
            {
                Message = result.FirstError?.Message ?? "GA4 verisi alınamadı.",
            };
    }

    public async Task<GoogleSearchConsoleDashboardWidgetDto> GetSearchConsoleWidgetAsync(
        CancellationToken ct = default)
    {
        var result = await _googleSearchConsoleService.GetDashboardWidgetAsync(ct);
        return result.IsSuccess
            ? result.Value
            : new GoogleSearchConsoleDashboardWidgetDto
            {
                Message = result.FirstError?.Message ?? "Search Console verisi alınamadı.",
            };
    }
}
