using System.Diagnostics;
using Automation.Api;
using Automation.Core;
using Automation.Integrations;
using Automation.Selenium;
namespace Automation.Testing;

public sealed class TestScope : IAsyncDisposable
{
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private readonly string name;
    private readonly IDisposable? disposableToken;
    private readonly HttpClient tokenHttp = new(new HttpClientHandler { AllowAutoRedirect = false });
    private bool disposed;
    public AutomationSettings Settings { get; }
    public ArtifactDirectory Artifacts { get; }
    public IEventSink Events { get; }
    public CleanupScope Cleanup { get; } = new();
    public BrowserSessions Browsers { get; }
    public HttpClient ApiHttp { get; }
    public TestData Data { get; }
    public TestScope(AutomationSettings settings, string name, int? seed = null)
    {
        settings.Validate();
        Settings = settings;
        this.name = name;
        Artifacts = new ArtifactDirectory(settings.ArtifactsPath, name);
        Events = new JsonEventSink(Artifacts.Path);
        Data = new TestData(seed ?? Random.Shared.Next());
        Events.Write("test.started", new { seed = Data.Seed });
        ITokenProvider tokens = settings.Auth.Mode switch
        {
            "StaticBearer" => new StaticTokenProvider(settings.Auth.Token),
            "ClientCredentials" => new ClientCredentialsTokenProvider(tokenHttp, settings.Auth),
            _ => new NoTokenProvider()
        };
        disposableToken = tokens as IDisposable;
        ApiHttp = ApiClientFactory.Create(new Uri(settings.ApiBaseUrl), tokens, Events);
        Browsers = new BrowserSessions(new BrowserFactory(settings.Browser), Path.Combine(Artifacts.Path, "downloads"));
    }
    public async Task CompleteAsync(string outcome, IReadOnlyList<string>? caseIds = null)
    {
        if (outcome == "Failed" && Browsers.Started) BrowserDiagnostics.Capture(Browsers.Current, Artifacts, Events);
        var failures = new List<Exception>();
        try { await DisposeAsync(); } catch (Exception ex) { failures.Add(ex); }
        var finalOutcome = failures.Count > 0 ? "Failed" : outcome;
        await new JsonResultPublisher(Artifacts.File("result.json"))
            .PublishAsync(new TestResult(name, finalOutcome, watch.Elapsed.TotalSeconds, caseIds ?? Array.Empty<string>()));
        if (failures.Count > 0) throw new AggregateException("Test cleanup failed; inspect original test failure too.", failures);
    }
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        var failures = new List<Exception>();
        try { await Cleanup.DisposeAsync(); } catch (Exception ex) { failures.Add(ex); }
        try { Browsers.Dispose(); } catch (Exception ex) { failures.Add(ex); }
        ApiHttp.Dispose();
        disposableToken?.Dispose();
        tokenHttp.Dispose();
        if (failures.Count > 0) throw new AggregateException(failures);
    }
}
