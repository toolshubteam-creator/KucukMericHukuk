using KucukMericHukuk.Core.DTOs.Article;

namespace KucukMericHukuk.Core.DTOs.Dashboard;

/// <summary>
/// Admin Dashboard "Son Makaleler" widget'ı için: toplam makale sayısı + son N yayınlanan makale.
/// </summary>
public class RecentArticlesWidgetDto
{
    public int TotalCount { get; set; }
    public IReadOnlyList<ArticleListDto> RecentArticles { get; set; } = Array.Empty<ArticleListDto>();
}
