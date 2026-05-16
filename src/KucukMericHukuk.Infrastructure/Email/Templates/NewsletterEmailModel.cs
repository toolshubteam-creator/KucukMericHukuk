namespace KucukMericHukuk.Infrastructure.Email.Templates;

/// <summary>
/// Faz 7.2b-1: Bülten HTML şablonuna geçirilen veri. URL'ler tam adres (host + culture + path) —
/// e-posta dış istemcide açıldığı için relative URL çalışmaz.
/// </summary>
public record NewsletterEmailModel(
    string ArticleTitle,
    string? ArticleExcerpt,
    string? FeaturedImageUrl,
    string ArticleUrl,
    string UnsubscribeUrl,
    string SiteName);
