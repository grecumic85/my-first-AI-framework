using System.Net;
using System.Text;
namespace Automation.Emulation;

public sealed record RequestTemplate(string Method, string Path, string Query, string? Body);
public static class RequestCapture
{
    // Creates emulator bindings from a generated-client invocation without reaching a real server.
    // Supply JSON appropriate for that operation's generated return type.
    public static async Task<RequestTemplate> CaptureAsync(Func<HttpClient, Task> call,
        string responseBody = "{}", HttpStatusCode responseStatus = HttpStatusCode.OK)
    {
        using var handler = new CaptureHandler(responseBody, responseStatus);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://capture.invalid/") };
        await call(http);
        return handler.Captured ?? throw new InvalidOperationException("The invocation did not issue a request.");
    }
    private sealed class CaptureHandler(string body, HttpStatusCode status) : HttpMessageHandler
    {
        public RequestTemplate? Captured { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (Captured is not null) throw new InvalidOperationException("Capture one API call at a time.");
            Captured = new RequestTemplate(request.Method.Method, request.RequestUri!.AbsolutePath,
                request.RequestUri.Query, request.Content is null ? null : await request.Content.ReadAsStringAsync(token));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
