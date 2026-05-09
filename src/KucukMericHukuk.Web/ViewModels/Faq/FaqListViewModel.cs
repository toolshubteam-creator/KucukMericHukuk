using KucukMericHukuk.Core.DTOs.Faq;

namespace KucukMericHukuk.Web.ViewModels.Faq;

public class FaqListViewModel
{
    public IReadOnlyList<FaqListDto> Faqs { get; init; } = [];
}
