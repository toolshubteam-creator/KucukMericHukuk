using KucukMericHukuk.Core.Constants;

namespace KucukMericHukuk.Core.Routing;

public static class PublicRouteSegments
{
    public static string Contact(string culture) => IsEnglish(culture) ? "contact" : "iletisim";
    public static string ContactSubmit(string culture) => IsEnglish(culture) ? "submit" : "gonder";
    public static string ContactThankYou(string culture) => IsEnglish(culture) ? "thank-you" : "tesekkurler";

    public static string Appointment(string culture) => IsEnglish(culture) ? "appointment" : "randevu";
    public static string AppointmentSubmit(string culture) => IsEnglish(culture) ? "submit" : "gonder";
    public static string AppointmentThankYou(string culture) => IsEnglish(culture) ? "thank-you" : "tesekkurler";

    public static string Articles(string culture) => IsEnglish(culture) ? "articles" : "makaleler";
    public static string Services(string culture) => IsEnglish(culture) ? "services" : "hizmetler";
    public static string Attorneys(string culture) => IsEnglish(culture) ? "attorneys" : "avukatlar";
    public static string Pages(string culture) => IsEnglish(culture) ? "pages" : "sayfalar";
    public static string Faqs(string culture) => IsEnglish(culture) ? "faq" : "sss";
    public static string Gallery(string culture) => IsEnglish(culture) ? "gallery" : "galeri";
    public static string Testimonials(string culture) => IsEnglish(culture) ? "testimonials" : "referanslar";
    public static string Subscriber(string culture) => IsEnglish(culture) ? "subscriber" : "abone";
    public static string Subscribe(string culture) => IsEnglish(culture) ? "subscribe" : "abone-ol";
    public static string Unsubscribe(string culture) => IsEnglish(culture) ? "unsubscribe" : "abonelikten-cik";

    public static string HomePath(string culture) => $"/{culture}/";
    public static string ContactPath(string culture) => $"/{culture}/{Contact(culture)}";
    public static string ContactSubmitPath(string culture) => $"{ContactPath(culture)}/{ContactSubmit(culture)}";
    public static string ContactThankYouPath(string culture) => $"{ContactPath(culture)}/{ContactThankYou(culture)}";
    public static string AppointmentPath(string culture) => $"/{culture}/{Appointment(culture)}";
    public static string AppointmentSubmitPath(string culture) => $"{AppointmentPath(culture)}/{AppointmentSubmit(culture)}";
    public static string AppointmentThankYouPath(string culture) => $"{AppointmentPath(culture)}/{AppointmentThankYou(culture)}";
    public static string ArticlesPath(string culture) => $"/{culture}/{Articles(culture)}";
    public static string ArticleDetailPath(string culture, string slug) => $"{ArticlesPath(culture)}/{slug}";
    public static string ServicesPath(string culture) => $"/{culture}/{Services(culture)}";
    public static string ServiceDetailPath(string culture, string slug) => $"{ServicesPath(culture)}/{slug}";
    public static string AttorneysPath(string culture) => $"/{culture}/{Attorneys(culture)}";
    public static string AttorneyDetailPath(string culture, string slug) => $"{AttorneysPath(culture)}/{slug}";
    public static string PagesPath(string culture) => $"/{culture}/{Pages(culture)}";
    public static string PageDetailPath(string culture, string slug) => $"{PagesPath(culture)}/{slug}";
    public static string FaqsPath(string culture) => $"/{culture}/{Faqs(culture)}";
    public static string GalleryPath(string culture) => $"/{culture}/{Gallery(culture)}";
    public static string TestimonialsPath(string culture) => $"/{culture}/{Testimonials(culture)}";
    public static string SubscribePath(string culture) => $"/{culture}/{Subscriber(culture)}/{Subscribe(culture)}";
    public static string UnsubscribePath(string culture, Guid token) => $"/{culture}/{Subscriber(culture)}/{Unsubscribe(culture)}/{token}";

    public static bool TryGetSluggedSegment(string segment, string culture, out string canonicalSegment)
    {
        var normalized = segment.ToLowerInvariant();
        var expected = new[]
        {
            (Turkish: "makaleler", English: "articles"),
            (Turkish: "hizmetler", English: "services"),
            (Turkish: "avukatlar", English: "attorneys"),
            (Turkish: "sayfalar", English: "pages")
        };

        foreach (var item in expected)
        {
            if (normalized == item.Turkish || normalized == item.English)
            {
                canonicalSegment = IsEnglish(culture) ? item.English : item.Turkish;
                return true;
            }
        }

        canonicalSegment = string.Empty;
        return false;
    }

    private static bool IsEnglish(string culture)
    {
        return string.Equals(culture, LanguageCodes.English, StringComparison.OrdinalIgnoreCase);
    }
}
