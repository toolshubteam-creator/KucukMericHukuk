namespace KucukMericHukuk.Core.Common;

public class SiteInfoOptions
{
    public const string SectionName = "SiteInfo";

    public string Name { get; set; } = "Küçükmeriç Hukuk Bürosu";
    public string Tagline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string DefaultOgImage { get; set; } = "/img/og-default.png";
    public string Locale { get; set; } = "tr_TR";
    public string TwitterHandle { get; set; } = string.Empty;
}
