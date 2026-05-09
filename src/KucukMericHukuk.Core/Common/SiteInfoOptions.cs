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

    // İletişim & Adres (LegalService schema için; müşteri verince doldurulur)
    public string Telephone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string AddressLocality { get; set; } = "Serdivan";
    public string AddressRegion { get; set; } = "Sakarya";
    public string PostalCode { get; set; } = string.Empty;
    public string AddressCountry { get; set; } = "TR";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string AreaServed { get; set; } = "Sakarya, Türkiye";
    public string OpeningHoursDescription { get; set; } = string.Empty;
}
