namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Shared;

public class PaginationViewModel
{
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public Func<int, string> GetPageUrl { get; set; } = _ => "#";
}
