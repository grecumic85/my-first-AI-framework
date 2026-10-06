using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Automation.Core;
namespace Automation.Api;

public interface ITokenProvider { Task<string?> GetAsync(CancellationToken token = default); }
public sealed class NoTokenProvider : ITokenProvider { public Task<string?> GetAsync(CancellationToken token = default) => Task.FromResult<string?>(null); }
public sealed class StaticTokenProvider(string value) : ITokenProvider { public Task<string?> GetAsync(CancellationToken token = default) => Task.FromResult<string?>(value); }

public sealed class ClientCredentialsTokenProvider(HttpClient client, AuthSettings settings) : ITokenProvider, IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? cached;
    private DateTimeOffset refreshAt;
    public async Task<string?> GetAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (cached is not null && DateTimeOffset.UtcNow < refreshAt) return cached;
            var endpoint = new Uri(settings.TokenEndpoint);
            if (endpoint.Scheme != "https" && !endpoint.IsLoopback) throw new InvalidOperationException("OAuth requires HTTPS outside loopback.");
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials", ["client_id"] = settings.ClientId,
                    ["client_secret"] = settings.ClientSecret, ["scope"] = settings.Scope
                })
            };
            using var response = await client.SendAsync(request, token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Token request failed ({(int)response.StatusCode}).", null, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Empty token response.");
            if (string.IsNullOrWhiteSpace(result.AccessToken) || result.ExpiresIn <= 0) throw new InvalidOperationException("Invalid token response.");
            cached = result.AccessToken;
            refreshAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, result.ExpiresIn - Math.Min(60, result.ExpiresIn / 10.0)));
            return cached;
        }
        finally { gate.Release(); }
    }
    public void Dispose() => gate.Dispose();
    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
