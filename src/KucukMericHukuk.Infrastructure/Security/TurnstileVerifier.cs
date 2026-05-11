using System.Text.Json;
using System.Text.Json.Serialization;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Infrastructure.Security;

public class TurnstileVerifier : ITurnstileVerifier
{
    private const string VerifyEndpoint = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;
    private readonly ILogger<TurnstileVerifier> _logger;

    public TurnstileVerifier(
        HttpClient httpClient,
        IOptions<TurnstileOptions> options,
        ILogger<TurnstileVerifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Turnstile verify: empty token");
            return false;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_options.VerifyTimeoutMs);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("secret", _options.SecretKey),
                new("response", token)
            };
            if (!string.IsNullOrEmpty(remoteIp))
                formData.Add(new("remoteip", remoteIp));

            using var content = new FormUrlEncodedContent(formData);
            using var response = await _httpClient.PostAsync(VerifyEndpoint, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Turnstile verify HTTP {Status}", response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(cts.Token);
            var result = JsonSerializer.Deserialize<TurnstileResponse>(body);

            if (result is null)
            {
                _logger.LogWarning("Turnstile verify: null deserialize result");
                return false;
            }

            if (!result.Success)
            {
                var errors = result.ErrorCodes is { Length: > 0 }
                    ? string.Join(",", result.ErrorCodes)
                    : "(none)";
                _logger.LogWarning("Turnstile verify failed: {Errors}", errors);
            }

            return result.Success;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Turnstile verify timeout after {Timeout}ms", _options.VerifyTimeoutMs);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Turnstile verify exception");
            return false;
        }
    }

    private sealed class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
    }
}
