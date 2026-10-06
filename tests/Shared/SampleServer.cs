using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using SampleApp;
namespace Automation.TestExamples;

public sealed class SampleServer : IAsyncDisposable
{
    private readonly WebApplication app;
    public Uri BaseUrl { get; }
    private SampleServer(WebApplication app)
    {
        this.app = app;
        BaseUrl = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single() + "/");
    }
    public static async Task<SampleServer> StartAsync()
    {
        var app = AppHost.Build(["--urls", "http://127.0.0.1:0", "--Logging:LogLevel:Default", "Warning"]);
        try { await app.StartAsync(); return new SampleServer(app); }
        catch { await app.DisposeAsync(); throw; }
    }
    public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
}
