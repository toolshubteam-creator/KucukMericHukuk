namespace KucukMericHukuk.Web.ViewModels.Common;

public class PaginationViewModel
{
    public int PageNumber { get; init; }
    public int TotalPages { get; init; }
    public string Action { get; init; } = "Index";
    public string Controller { get; init; } = "";
    public IDictionary<string, string> QueryParams { get; init; } = new Dictionary<string, string>();
}
