using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Automation.Core;
namespace Automation.Api;

public sealed class BearerHandler(ITokenProvider tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var value = await tokens.GetAsync(token).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(value)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", value);
        return await base.SendAsync(request, token).ConfigureAwait(false);
    }
}

public sealed class HttpAuditHandler(IEventSink events) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, token).ConfigureAwait(false);
            // Never log headers, query strings, request/response bodies, or credentials.
            events.Write("http.response", new { method = request.Method.Method, status = (int)response.StatusCode, elapsedMs = watch.ElapsedMilliseconds });
            return response;
        }
        catch (Exception ex)
        {
            events.Write("http.failure", new { method = request.Method.Method, exceptionType = ex.GetType().Name, elapsedMs = watch.ElapsedMilliseconds });
            throw;
        }
    }
}

public sealed class ReadRetryHandler(int maximumRetries = 3) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        // Writes are never replayed automatically. Even idempotent writes can trigger application side effects.
        if (request.Content is not null || (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head))
            return await base.SendAsync(request, token).ConfigureAwait(false);
        for (var attempt = 0; ; attempt++)
        {
            using var copy = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version, VersionPolicy = request.VersionPolicy };
            foreach (var header in request.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
            foreach (var option in request.Options) copy.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            HttpResponseMessage response;
            try { response = await base.SendAsync(copy, token).ConfigureAwait(false); }
            catch (HttpRequestException) when (attempt < maximumRetries)
            { await Task.Delay(Delay(attempt), token).ConfigureAwait(false); continue; }
            if (attempt >= maximumRetries || !IsTransient(response.StatusCode)) return response;
            var retryAfter = response.Headers.RetryAfter;
            var pause = retryAfter?.Delta ?? (retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : Delay(attempt));
            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Clamp(pause.TotalMilliseconds, 0, 30000)), token).ConfigureAwait(false);
        }
    }
    private static bool IsTransient(HttpStatusCode status) => (int)status is 408 or 429 or 500 or 502 or 503 or 504;
    private static TimeSpan Delay(int attempt) => TimeSpan.FromMilliseconds(100 * Math.Pow(2, Math.Min(attempt, 8)) + Random.Shared.Next(25));
}

public static class ApiClientFactory
{
    public static HttpClient Create(Uri baseUrl, ITokenProvider tokens, IEventSink events, HttpMessageHandler? transport = null)
    {
        var pipeline = new HttpAuditHandler(events)
        {
            InnerHandler = new ReadRetryHandler
            {
                InnerHandler = new BearerHandler(tokens)
                { InnerHandler = transport ?? new HttpClientHandler { AllowAutoRedirect = false } }
            }
        };
        return new HttpClient(pipeline) { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(60) };
    }
}
