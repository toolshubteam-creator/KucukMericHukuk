namespace KucukMericHukuk.Web.ViewModels.Common;

public record BreadcrumbItem(string Label, string? Url)
{
    public bool IsCurrent => Url is null;
}
