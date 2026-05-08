using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Service;

namespace KucukMericHukuk.Web.ViewModels.Home;

public class HomeIndexViewModel
{
    public IReadOnlyList<ServiceListDto> FeaturedServices { get; init; } = [];
    public IReadOnlyList<AttorneyListDto> FeaturedAttorneys { get; init; } = [];
    public IReadOnlyList<ArticleListDto> RecentArticles { get; init; } = [];
}
