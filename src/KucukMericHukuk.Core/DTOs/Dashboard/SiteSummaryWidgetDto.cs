namespace KucukMericHukuk.Core.DTOs.Dashboard;

/// <summary>
/// Admin Dashboard "Site Özeti" widget'ı için: içerik entity'lerinin aktif kayıt sayıları.
/// Tüm sayımlar soft-delete query filter'ından geçer (yalnızca silinmemiş kayıtlar).
/// </summary>
public class SiteSummaryWidgetDto
{
    public int ArticleCount { get; set; }
    public int ServiceCount { get; set; }
    public int AttorneyCount { get; set; }
    public int PageCount { get; set; }
    public int MediaCount { get; set; }
    public int FaqCount { get; set; }
    public int TestimonialCount { get; set; }
}
