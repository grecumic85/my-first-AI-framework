using Automation.Core;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
namespace Automation.Emulation;

public sealed record CapturedRequest(string Method, string Path, string? Body);
public interface IHttpEmulator : IAsyncDisposable
{
    Uri BaseUrl { get; }
    Guid Stub(string method, string path, int status = 200, string body = "{}", TimeSpan? delay = null);
    Task<IReadOnlyList<CapturedRequest>> WaitForRequestsAsync(Guid mappingId, int minimumCount, TimeSpan timeout, CancellationToken token = default);
}

// Each test owns a random-port server, its request journal and all mappings. No global reset races.
public sealed class HttpEmulator : IHttpEmulator
{
    private readonly WireMockServer server = WireMockServer.Start();
    public Uri BaseUrl => new(server.Url! + "/");
    public Guid Stub(string method, string path, int status = 200, string body = "{}", TimeSpan? delay = null)
    {
        var response = Response.Create().WithStatusCode(status).WithHeader("Content-Type", "application/json").WithBody(body);
        if (delay is { } duration) response.WithDelay(duration);
        var id = Guid.NewGuid();
        server.Given(Request.Create().WithPath(path).UsingMethod(method)).WithGuid(id).RespondWith(response);
        return id;
    }
    public void Sequence(string method, string path, params (int Status, string Body)[] responses)
    {
        if (responses.Length == 0) throw new ArgumentException("At least one response is required.");
        var scenario = Guid.NewGuid().ToString("N");
        for (var i = 0; i < responses.Length; i++)
        {
            var mapping = server.Given(Request.Create().WithPath(path).UsingMethod(method)).InScenario(scenario);
            if (i == 0) mapping.WhenStateIs((string)null!);
            else mapping.WhenStateIs(i.ToString());
            if (i < responses.Length - 1) mapping.WillSetStateTo((i + 1).ToString());
            mapping.RespondWith(Response.Create().WithStatusCode(responses[i].Status).WithHeader("Content-Type", "application/json").WithBody(responses[i].Body));
        }
    }
    public IReadOnlyList<CapturedRequest> Requests(Guid mappingId) => server.LogEntries
        .Where(e => e.MappingGuid == mappingId)
        .Select(e => new CapturedRequest(e.RequestMessage.Method, e.RequestMessage.Path, e.RequestMessage.Body)).ToArray();
    public Task<IReadOnlyList<CapturedRequest>> WaitForRequestsAsync(Guid mappingId, int minimumCount, TimeSpan timeout, CancellationToken token = default)
    {
        if (minimumCount < 1) throw new ArgumentOutOfRangeException(nameof(minimumCount));
        return Poll.UntilAsync(_ => Task.FromResult(Requests(mappingId)), requests => requests.Count >= minimumCount,
            timeout, TimeSpan.FromMilliseconds(50), "Emulator requests", token);
    }
    public async Task AssertNoRequestsAsync(Guid mappingId, TimeSpan observationWindow, CancellationToken token = default)
    {
        if (observationWindow <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(observationWindow));
        var watch = System.Diagnostics.Stopwatch.StartNew();
        do
        {
            token.ThrowIfCancellationRequested();
            if (Requests(mappingId).Count != 0) throw new InvalidOperationException("Unexpected emulator request.");
            if (watch.Elapsed >= observationWindow) break;
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(50, (observationWindow - watch.Elapsed).TotalMilliseconds)), token);
        } while (true);
    }
    public void Remove(Guid mappingId) => server.DeleteMapping(mappingId);
    public ValueTask DisposeAsync() { server.Dispose(); return ValueTask.CompletedTask; }
}
