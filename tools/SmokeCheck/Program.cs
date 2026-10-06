using System.Text.Json;
using Automation.Api;
using Automation.Core;
using Automation.Database;
using Automation.Documents;
using Automation.Emulation;
using Automation.Generated.Sample;
using Automation.Selenium;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;

// A runnable smoke harness independent of test discovery. It never contacts your configured app.
var results = new List<object>();
var artifacts = new ArtifactDirectory("artifacts", "smoke-check");
await using var app = SampleApp.AppHost.Build(["--urls", "http://127.0.0.1:0", "--Logging:LogLevel:Default", "Warning"]);
await app.StartAsync();
try
{
    var url = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single() + "/");
    using var http = ApiClientFactory.Create(url, new NoTokenProvider(), new JsonEventSink(artifacts.Path));
    ISampleClient client = new SampleClient(http);
    await Check("generated client CRUD", async () =>
    {
        Expect((await client.GetHealthAsync()).Status == "ok", "Health");
        var item = await client.CreateItemAsync(new CreateItem { Name = "smoke-check" });
        Expect((await client.GetItemAsync(item.Id)).Name == "smoke-check", "Round trip");
        await client.DeleteItemAsync(item.Id);
        try { await client.GetItemAsync(item.Id); throw new Exception("Missing expected 404"); }
        catch (ApiException ex) when (ex.StatusCode == 404) { }
    });
    await Check("polling and timeout", async () =>
    {
        var count = 0;
        await Poll.UntilAsync(_ => Task.FromResult(++count), n => n == 3, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1), "counter");
        Expect(count == 3, "Polling");
        try
        {
            await Poll.UntilAsync(_ => Task.FromResult(false), v => v, TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(5), "deadline");
            throw new Exception("Missing expected timeout");
        }
        catch (TimeoutException) { }
    });
    await Check("reverse cleanup continues after failure", async () =>
    {
        var cleanup = new CleanupScope();
        var order = new List<int>();
        cleanup.Register("first", _ => { order.Add(1); return Task.CompletedTask; });
        cleanup.Register("second", _ => { order.Add(2); throw new InvalidOperationException(); });
        try { await cleanup.DisposeAsync(); throw new Exception("Missing cleanup failure"); }
        catch (AggregateException) { }
        Expect(order.SequenceEqual([2, 1]), "Cleanup ordering");
    });
    await Check("emulator request journal", async () =>
    {
        await using var emulator = new HttpEmulator();
        var id = emulator.Stub("POST", "/webhook", 202);
        using var sender = new HttpClient { BaseAddress = emulator.BaseUrl };
        using var response = await sender.PostAsync("webhook", new StringContent("test-body"));
        var requests = await emulator.WaitForRequestsAsync(id, 1, TimeSpan.FromSeconds(2));
        Expect((int)response.StatusCode == 202 && requests[0].Body == "test-body", "Journal");
    });
    await Check("stateful emulator and HTTP retry", async () =>
    {
        await using var emulator = new HttpEmulator();
        emulator.Sequence("GET", "/status", (503, "{}"), (200, "{\"ready\":true}"));
        using var sender = ApiClientFactory.Create(emulator.BaseUrl, new NoTokenProvider(), new NullEventSink());
        var value = await new JsonApiClient(sender).GetAsync<JsonElement>("status");
        Expect(value.GetProperty("ready").GetBoolean(), "Retry recovery");
    });
    await Check("admin emulator resource lifecycle", async () =>
    {
        var admin = new AdminEmulatorClient(http);
        var cleanup = new CleanupScope();
        await admin.SetOwnedTransactionAsync("smoke-setting", new { success = true }, cleanup);
        await admin.CreateOwnedBenchmarkAsync(new Benchmark(Guid.NewGuid(), new() { ["kind"] = "sample" }, JsonSerializer.SerializeToElement(new { value = 10 })), cleanup);
        Expect((await admin.GetTransactionAsync("smoke-setting")).GetProperty("success").GetBoolean(), "Settings round trip");
        Expect((await admin.GetBenchmarksAsync()).Length == 1, "Benchmark round trip");
        await cleanup.DisposeAsync();
        Expect((await admin.GetBenchmarksAsync()).Length == 0, "Benchmark cleanup");
    });
    await Check("SQLite repositories and rollback", async () =>
    {
        var factory = DatabaseFactory.Create(new DatabaseSettings { Provider = "Sqlite", ConnectionString = $"Data Source={artifacts.File("smoke.db")}" });
        await using (var db = factory.CreateDbContext()) await db.Database.EnsureCreatedAsync();
        var repository = new Repository<ExampleDbContext, SampleRecord>(factory, true);
        var id = Guid.NewGuid();
        await repository.AddAsync(new SampleRecord { Id = id, Name = "database smoke" });
        Expect((await repository.WaitForAsync(r => r.Id == id, TimeSpan.FromSeconds(1))).Name == "database smoke", "Repository");
        await new DatabaseTransaction<ExampleDbContext>(factory).ExecuteAsync((db, _) =>
        { db.Records.Add(new SampleRecord { Id = Guid.NewGuid(), Name = "rollback" }); return Task.CompletedTask; });
        Expect((await repository.FindAsync(r => r.Name == "rollback")).Count == 0, "Rollback");
        await repository.DeleteAsync(r => r.Id == id);
    });
    await Check("PDF text extraction", () =>
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(300, 300);
        page.AddText("Sample invoice", 12, new PdfPoint(20, 200), builder.AddStandard14Font(Standard14Font.Helvetica));
        var file = artifacts.File("sample.pdf");
        File.WriteAllBytes(file, builder.Build());
        Expect(new PdfTextExtractor().Read(file).Contains("Sample invoice"), "PDF");
        return Task.CompletedTask;
    });
    if (args.Contains("--ui"))
    {
        await Check("real Chrome page, waits, network and download", async () =>
        {
            var downloads = Path.Combine(artifacts.Path, "downloads");
            using var sessions = new BrowserSessions(new BrowserFactory(new BrowserSettings()), downloads);
            var driver = sessions.Current;
            driver.Navigate().GoToUrl(url);
            var wait = new BrowserWait(driver, new WaitSettings());
            wait.Visible(By.CssSelector("[data-testid=page-title]"));
            NetworkTracker.Install(driver);
            wait.Visible(By.Id("name")).SendKeys("UI smoke");
            wait.Click(By.CssSelector("[data-testid=add-item]"));
            wait.NetworkIdle();
            wait.Gone(By.Id("loading"));
            Expect(wait.StableList(By.CssSelector("[data-testid=item]"), requireText: true).Any(e => e.Text == "UI smoke"), "UI item");
            wait.Click(By.CssSelector("[data-testid=download]"));
            var file = await Downloads.WaitForFileAsync(Directory.GetDirectories(downloads).Single(), "sample.csv", TimeSpan.FromSeconds(15));
            Expect((await File.ReadAllTextAsync(file)).Contains("1,Sample"), "CSV download");
        });
    }
}
finally
{
    await app.StopAsync();
    await File.WriteAllTextAsync(artifacts.File("smoke-results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Results: {artifacts.Path}");
}
if (results.Any(r => r is SmokeResult { Passed: false })) Environment.ExitCode = 1;

async Task Check(string name, Func<Task> action)
{
    try { await action(); results.Add(new SmokeResult(name, true, null)); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { results.Add(new SmokeResult(name, false, ex.ToString())); Console.WriteLine($"FAIL {name}: {ex}"); }
}
static void Expect(bool result, string description) { if (!result) throw new InvalidOperationException(description); }
record SmokeResult(string Name, bool Passed, string? Failure);
