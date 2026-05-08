using KucukMericHukuk.Core.DTOs.Attorney;

namespace KucukMericHukuk.Web.ViewModels.Attorney;

public class AttorneyListViewModel
{
    public IReadOnlyList<AttorneyListDto> Attorneys { get; init; } = [];
}
