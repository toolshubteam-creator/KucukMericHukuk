namespace KucukMericHukuk.Core.Common;

/// <summary>
/// SiteSettings key sabitleri. Property adı = appsettings.json/SiteInfoOptions field adı
/// (IConfigureOptions hydration aynı isim eşleşmesini bekler).
/// </summary>
public static class SiteSettingKeys
{
    public static class Groups
    {
        public const string SiteInfo = "SiteInfo";
        public const string Seo = "Seo";
        public const string Integration = "Integration";
    }

    public static class DataTypes
    {
        public const string String = "string";
        public const string Url = "url";
        public const string Email = "email";
        public const string Decimal = "decimal";
        public const string Int = "int";
        public const string Bool = "bool";
    }

    // SiteInfo group — appsettings SiteInfo section property names ile eşleşir
    public const string Name = "Name";
    public const string Tagline = "Tagline";
    public const string Description = "Description";
    public const string BaseUrl = "BaseUrl";
    public const string Locale = "Locale";
    public const string TwitterHandle = "TwitterHandle";
    public const string Telephone = "Telephone";
    public const string Email = "Email";
    public const string StreetAddress = "StreetAddress";
    public const string AddressLocality = "AddressLocality";
    public const string AddressRegion = "AddressRegion";
    public const string PostalCode = "PostalCode";
    public const string AddressCountry = "AddressCountry";
    public const string Latitude = "Latitude";
    public const string Longitude = "Longitude";
    public const string AreaServed = "AreaServed";
    public const string OpeningHoursDescription = "OpeningHoursDescription";

    // Seo group
    public const string DefaultOgImage = "DefaultOgImage";
    public const string GoogleSearchConsoleVerification = "GoogleSearchConsoleVerification";

    // Integration group
    public const string GoogleAnalyticsId = "GoogleAnalyticsId";
    public const string GoogleTagManagerId = "GoogleTagManagerId";
    public const string MicrosoftClarityId = "MicrosoftClarityId";
    public const string FacebookPixelId = "FacebookPixelId";
}
