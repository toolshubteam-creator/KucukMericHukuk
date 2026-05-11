using FluentValidation;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.SiteSetting;

namespace KucukMericHukuk.Business.Validators;

public class SiteSettingUpdateInputValidator : AbstractValidator<SiteSettingUpdateInput>
{
    // Key adı → DataType eşlemesi. SiteSettingKeys ile aynı kaynak — eklenen yeni key burada da güncellenmeli.
    private static readonly Dictionary<string, string> KeyDataTypes = new()
    {
        [SiteSettingKeys.BaseUrl] = SiteSettingKeys.DataTypes.Url,
        [SiteSettingKeys.Email] = SiteSettingKeys.DataTypes.Email,
        [SiteSettingKeys.Latitude] = SiteSettingKeys.DataTypes.Decimal,
        [SiteSettingKeys.Longitude] = SiteSettingKeys.DataTypes.Decimal,
    };

    public SiteSettingUpdateInputValidator()
    {
        RuleFor(x => x.Group)
            .NotEmpty().WithMessage("Grup adı zorunludur.")
            .MaximumLength(50);

        RuleFor(x => x.Values)
            .NotNull().WithMessage("Değer listesi boş olamaz.");

        RuleFor(x => x).Custom((input, ctx) =>
        {
            if (input.Values == null) return;

            foreach (var (key, value) in input.Values)
            {
                if (string.IsNullOrWhiteSpace(value)) continue; // boş bırakılabilir

                if (!KeyDataTypes.TryGetValue(key, out var dataType)) continue;

                switch (dataType)
                {
                    case SiteSettingKeys.DataTypes.Email:
                        if (!IsValidEmail(value))
                            ctx.AddFailure($"Values[{key}]", $"Geçerli bir e-posta adresi girin: {key}");
                        break;

                    case SiteSettingKeys.DataTypes.Url:
                        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                            ctx.AddFailure($"Values[{key}]", $"Geçerli bir URL girin (http/https): {key}");
                        break;

                    case SiteSettingKeys.DataTypes.Decimal:
                        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var d))
                        {
                            ctx.AddFailure($"Values[{key}]", $"Geçerli bir ondalık sayı girin: {key}");
                            break;
                        }
                        // Latitude / Longitude için makul aralık kontrolü
                        if (key == SiteSettingKeys.Latitude && (d < -90m || d > 90m))
                            ctx.AddFailure($"Values[{key}]", "Latitude -90 ile 90 arasında olmalıdır.");
                        if (key == SiteSettingKeys.Longitude && (d < -180m || d > 180m))
                            ctx.AddFailure($"Values[{key}]", "Longitude -180 ile 180 arasında olmalıdır.");
                        break;
                }
            }
        });
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
