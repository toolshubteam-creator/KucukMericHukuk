namespace KucukMericHukuk.Core.Interfaces.Services;

public interface IHtmlSanitizerService
{
    string Sanitize(string? rawHtml);
}
