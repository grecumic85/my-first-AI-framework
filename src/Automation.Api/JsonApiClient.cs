using System.Net;
using System.Net.Http.Json;
namespace Automation.Api;

public sealed class ApiFailureException(HttpStatusCode status, string method)
    : HttpRequestException($"API {method} failed with status {(int)status}.", null, status);

public class JsonApiClient(HttpClient client)
{
    protected HttpClient Client { get; } = client;
    public async Task<T> GetAsync<T>(string path, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Relative(path));
        using var response = await Client.SendAsync(request, token);
        Check(response, "GET");
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token) ?? throw new InvalidOperationException("Empty JSON response.");
    }
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Relative(path)) { Content = JsonContent.Create(body) };
        using var response = await Client.SendAsync(request, token);
        Check(response, "POST");
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: token) ?? throw new InvalidOperationException("Empty JSON response.");
    }
    public async Task SendAsync(HttpMethod method, string path, object? body = null, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(method, Relative(path)) { Content = body is null ? null : JsonContent.Create(body) };
        using var response = await Client.SendAsync(request, token);
        Check(response, method.Method);
    }
    private static Uri Relative(string path)
    {
        if (path.StartsWith('/') || path.Contains('\\') || Uri.TryCreate(path, UriKind.Absolute, out _))
            throw new ArgumentException("Use a relative API path without a leading slash.");
        return new Uri(path, UriKind.Relative);
    }
    private static void Check(HttpResponseMessage response, string method)
    { if (!response.IsSuccessStatusCode) throw new ApiFailureException(response.StatusCode, method); }
}
