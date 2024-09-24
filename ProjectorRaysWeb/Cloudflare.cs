using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Refit;

namespace ProjectorRaysWeb.CloudflareTurnstile;

public class CloudflareTurnstileSettings
{
    [Required]
    public string BaseUrl { get; set; } = null!;

    [Required]
    public string SiteKey { get; set; } = null!;

    [Required]
    public string SecretKey { get; set; } = null!;
}

public class CloudflareTurnstileProvider(IOptions<CloudflareTurnstileSettings> turnstileOptions,
    ICloudflareTurnstileClient client)
{
    private readonly CloudflareTurnstileSettings _turnstileSettings = turnstileOptions.Value;

    public async Task<CloudflareTurnstileVerifyResult> Verify(string token, string? idempotencyKey = null,
        IPAddress? userIpAddress = null, CancellationToken ct = default)
    {
        CloudflareTurnstileVerifyRequestModel requestModel = new(_turnstileSettings.SecretKey, token, userIpAddress?.ToString(), idempotencyKey);

        CloudflareTurnstileVerifyResult result = await client
                .Verify(requestModel, ct)
                .ConfigureAwait(false);

        return result;
    }
}

public record class CloudflareTurnstileVerifyRequestModel(
    // https://developers.cloudflare.com/turnstile/get-started/server-side-validation
    [property: JsonPropertyName("secret")] string SecretKey,
    [property: JsonPropertyName("response")] string Token,
    [property: JsonPropertyName("remoteip")] string? UserIpAddress,
    [property: JsonPropertyName("idempotency_key")] string? IdempotencyKey);

public interface ICloudflareTurnstileClient
{
    [Post("/siteverify")]
    [Headers("Content-Type: application/json")]
    public Task<CloudflareTurnstileVerifyResult> Verify(CloudflareTurnstileVerifyRequestModel requestModel,
          CancellationToken ct);
}

public record class CloudflareTurnstileVerifyResult(
    // https://developers.cloudflare.com/turnstile/get-started/server-side-validation/
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error-codes")] string[] ErrorCodes,
    [property: JsonPropertyName("challenge_ts")] DateTimeOffset On,
    [property: JsonPropertyName("hostname")] string Hostname
    );

public static class CloudflareTurnstileRegistration
{
    public static IServiceCollection AddCloudflareTurnstile(
        this IServiceCollection services, IConfigurationSection configurationSection)
    {
        // configure
        services.Configure<CloudflareTurnstileSettings>(configurationSection);

        // read url required for refit
        string? clientBaseUrl = configurationSection.GetValue<string>(nameof(CloudflareTurnstileSettings.BaseUrl));
        if (string.IsNullOrWhiteSpace(clientBaseUrl))
        {
            throw new InvalidOperationException($"Cloudflare Turnstile {nameof(CloudflareTurnstileSettings.BaseUrl)} is required.");
        }

        // in this sample the provider can be a singleton
        services.AddSingleton<CloudflareTurnstileProvider>();

        // add client
        services.AddRefitClient<ICloudflareTurnstileClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(clientBaseUrl));

        // return
        return services;
    }
}