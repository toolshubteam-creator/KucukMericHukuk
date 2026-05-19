using System.Text;
using Google.Apis.Auth.OAuth2;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Google;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Google;

public class GoogleApiClient : IGoogleApiClient
{
    private readonly GoogleIntegrationOptions _options;
    private readonly ISiteSettingsService _siteSettings;
    private readonly ILogger<GoogleApiClient> _logger;

    public GoogleApiClient(
        IOptions<GoogleIntegrationOptions> options,
        ISiteSettingsService siteSettings,
        ILogger<GoogleApiClient> logger)
    {
        _options = options.Value;
        _siteSettings = siteSettings;
        _logger = logger;
    }

    public async Task<Result<GoogleApiClientStatusDto>> GetStatusAsync(CancellationToken ct = default)
    {
        var source = await ResolveCredentialSourceAsync(ct);

        var status = new GoogleApiClientStatusDto
        {
            ApplicationName = _options.ApplicationName,
            IsConfigured = source is not null,
            CredentialSource = source?.Name,
            Message = source is null
                ? "Google API credential yapılandırılmamış."
                : $"Google API credential kaynağı hazır: {source.Name}."
        };

        return Result.Success(status);
    }

    public async Task<Result<string>> GetAccessTokenAsync(
        IReadOnlyCollection<string> scopes,
        CancellationToken ct = default)
    {
        if (scopes.Count == 0)
        {
            return Result.Failure<string>(new Error(
                ErrorCodes.GoogleIntegration.CredentialInvalid,
                "Google API scope listesi boş olamaz."));
        }

        var source = await ResolveCredentialSourceAsync(ct);
        if (source is null)
        {
            return Result.Failure<string>(new Error(
                ErrorCodes.GoogleIntegration.CredentialMissing,
                "Google API credential yapılandırılmamış."));
        }

        try
        {
            var credential = CredentialFactory
                .FromJson(source.Json, JsonCredentialParameters.ServiceAccountCredentialType)
                .CreateScoped(scopes);

            var token = await credential.UnderlyingCredential
                .GetAccessTokenForRequestAsync(cancellationToken: ct);

            return Result.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google API access token alınamadı. Source={Source}", source.Name);
            return Result.Failure<string>(new Error(
                ErrorCodes.GoogleIntegration.TokenRequestFailed,
                "Google API access token alınamadı. Credential değerini ve yetkileri kontrol edin."));
        }
    }

    private async Task<CredentialSource?> ResolveCredentialSourceAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountJson))
            return new CredentialSource("GoogleIntegration__ServiceAccountJson", _options.ServiceAccountJson);

        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountJsonBase64))
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(_options.ServiceAccountJsonBase64));
                return new CredentialSource("GoogleIntegration__ServiceAccountJsonBase64", json);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "GoogleIntegration__ServiceAccountJsonBase64 geçersiz base64.");
                return new CredentialSource("GoogleIntegration__ServiceAccountJsonBase64", string.Empty);
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountFilePath))
        {
            try
            {
                if (File.Exists(_options.ServiceAccountFilePath))
                {
                    var json = await File.ReadAllTextAsync(_options.ServiceAccountFilePath, ct);
                    return new CredentialSource("GoogleIntegration__ServiceAccountFilePath", json);
                }

                _logger.LogWarning("Google service account dosyası bulunamadı: {Path}",
                    _options.ServiceAccountFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google service account dosyası okunamadı: {Path}",
                    _options.ServiceAccountFilePath);
            }
        }

        var dbValue = await _siteSettings.GetValueAsync(SiteSettingKeys.GoogleServiceAccountJson, ct);
        if (dbValue.IsSuccess && !string.IsNullOrWhiteSpace(dbValue.Value))
            return new CredentialSource("SiteSettings.GoogleServiceAccountJson", dbValue.Value);

        return null;
    }

    private sealed record CredentialSource(string Name, string Json);
}
