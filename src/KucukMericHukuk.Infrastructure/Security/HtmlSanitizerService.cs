using Ganss.Xss;
using KucukMericHukuk.Core.Interfaces.Services;

namespace KucukMericHukuk.Infrastructure.Security;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedAttributes.Clear();

        foreach (var tag in new[]
        {
            "p", "br", "strong", "em", "u", "s",
            "a", "ul", "ol", "li",
            "h2", "h3", "h4",
            "blockquote", "code", "pre",
            "img"
        })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");

        // <img> attribute whitelist (Faz 3.3): src, alt, width, height, class.
        // style YASAK (XSS yüzeyi genişlemesin); on* zaten Ganss.Xss tarafından strip edilir.
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");
        _sanitizer.AllowedAttributes.Add("width");
        _sanitizer.AllowedAttributes.Add("height");
        _sanitizer.AllowedAttributes.Add("class");

        // HTTPS-only (Faz 3.3 kararı): http kaldırıldı, mixed-content engellenir.
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.AllowedSchemes.Add("tel");

        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedClasses.Clear();
    }

    public string Sanitize(string? rawHtml)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
            return string.Empty;

        return _sanitizer.Sanitize(rawHtml);
    }
}
