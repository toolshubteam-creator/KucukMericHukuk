using System.Net;

namespace KucukMericHukuk.Infrastructure.Email.Templates;

/// <summary>
/// Faz 7.2b-1: Bülten HTML şablonu — inline CSS (e-posta istemcileri external CSS desteklemez).
/// ContactMessageService.BuildReplyEmailBody pattern'i ile tutarlı (static method, string concat).
/// KVKK: alt bilgide token tabanlı abonelikten çıkış linki zorunlu.
/// </summary>
public static class NewsletterEmailTemplate
{
    public static string Render(NewsletterEmailModel model)
    {
        var title = WebUtility.HtmlEncode(model.ArticleTitle);
        var excerpt = string.IsNullOrWhiteSpace(model.ArticleExcerpt)
            ? null
            : WebUtility.HtmlEncode(model.ArticleExcerpt);
        var siteName = WebUtility.HtmlEncode(model.SiteName);
        var articleUrl = WebUtility.HtmlEncode(model.ArticleUrl);
        var unsubscribeUrl = WebUtility.HtmlEncode(model.UnsubscribeUrl);

        var imageBlock = string.IsNullOrWhiteSpace(model.FeaturedImageUrl)
            ? string.Empty
            : $@"<img src=""{WebUtility.HtmlEncode(model.FeaturedImageUrl)}"" alt=""{title}""
                 style=""max-width:100%;height:auto;display:block;margin:0 0 24px 0;border-radius:8px;"" />";

        var excerptBlock = excerpt is null
            ? string.Empty
            : $@"<p style=""color:#2C2C2A;font-size:16px;line-height:1.6;margin:0 0 24px 0;"">{excerpt}</p>";

        return $@"<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
    <title>{title}</title>
</head>
<body style=""margin:0;padding:0;background-color:#FAF7F2;font-family:Arial,Helvetica,sans-serif;color:#2C2C2A;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FAF7F2;padding:24px 0;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0""
                       style=""background-color:#FFFFFF;border:1px solid #E5E0D5;border-radius:8px;padding:32px;max-width:600px;"">
                    <tr>
                        <td>
                            <p style=""font-family:Georgia,'Times New Roman',serif;color:#0F2A23;font-size:14px;letter-spacing:0.05em;text-transform:uppercase;margin:0 0 8px 0;"">{siteName}</p>
                            <h1 style=""font-family:Georgia,'Times New Roman',serif;color:#0F2A23;font-size:24px;line-height:1.3;margin:0 0 16px 0;"">{title}</h1>
                            {imageBlock}
                            {excerptBlock}
                            <p style=""margin:24px 0;"">
                                <a href=""{articleUrl}""
                                   style=""display:inline-block;background-color:#C9A961;color:#0F2A23;text-decoration:none;padding:12px 28px;border-radius:4px;font-weight:bold;"">
                                    Devamını oku
                                </a>
                            </p>
                            <hr style=""border:none;border-top:1px solid #E5E0D5;margin:32px 0 16px 0;"" />
                            <p style=""font-size:12px;color:#6B6B6B;line-height:1.5;margin:0;"">
                                Bu e-postayı {siteName} bülten aboneliğiniz nedeniyle aldınız.
                                Bültenden çıkmak için
                                <a href=""{unsubscribeUrl}"" style=""color:#7B6232;text-decoration:underline;"">buraya tıklayın</a>.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
