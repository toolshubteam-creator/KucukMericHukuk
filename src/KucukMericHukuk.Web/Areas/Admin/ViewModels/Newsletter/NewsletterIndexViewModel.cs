using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Newsletter;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Newsletter;

public class NewsletterIndexViewModel
{
    public IReadOnlyList<PendingArticleDto> Pending { get; set; } = new List<PendingArticleDto>();
    public PagedResult<NewsletterJobListDto> History { get; set; } =
        new PagedResult<NewsletterJobListDto>(new List<NewsletterJobListDto>(), 0, 1, 20);
}
