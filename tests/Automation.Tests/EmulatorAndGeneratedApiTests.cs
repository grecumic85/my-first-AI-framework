using System.Text.Json;
using Automation.Api;
using Automation.Core;
using Automation.Emulation;
using Automation.Generated.Sample;
using NUnit.Framework;
namespace Automation.TestExamples;

[TestFixture]
public sealed class EmulatorAndGeneratedApiTests
{
    [Test]
    public async Task Http_emulator_captures_webhook_requests_and_body()
    {
        await using var emulator = new HttpEmulator();
        var mapping = emulator.Stub("POST", "/webhook", 202);
        using var http = new HttpClient { BaseAddress = emulator.BaseUrl };
        using var response = await http.PostAsync("webhook", new StringContent("payload"));
        var requests = await emulator.WaitForRequestsAsync(mapping, 1, TimeSpan.FromSeconds(2));
        Assert.That((int)response.StatusCode, Is.EqualTo(202));
        Assert.That(requests[0].Body, Is.EqualTo("payload"));
    }
    [Test]
    public async Task Stateful_fault_sequence_recovers_after_transient_error()
    {
        await using var emulator = new HttpEmulator();
        emulator.Sequence("GET", "/status", (503, "{}"), (200, "{\"ready\":true}"));
        using var http = ApiClientFactory.Create(emulator.BaseUrl, new NoTokenProvider(), new NullEventSink());
        var body = await new JsonApiClient(http).GetAsync<JsonElement>("status");
        Assert.That(body.GetProperty("ready").GetBoolean(), Is.True);
    }
    [Test]
    public async Task Separate_emulators_do_not_reset_each_other()
    {
        await using var first = new HttpEmulator();
        await using var second = new HttpEmulator();
        first.Stub("GET", "/owned", 201);
        second.Stub("GET", "/owned", 202);
        using var a = new HttpClient { BaseAddress = first.BaseUrl };
        using var b = new HttpClient { BaseAddress = second.BaseUrl };
        using var ra = await a.GetAsync("owned");
        using var rb = await b.GetAsync("owned");
        Assert.That((int)ra.StatusCode, Is.EqualTo(201));
        Assert.That((int)rb.StatusCode, Is.EqualTo(202));
    }
    [Test]
    public async Task Negative_request_assertion_observes_the_full_window()
    {
        await using var emulator = new HttpEmulator();
        var id = emulator.Stub("POST", "/unexpected");
        using var http = new HttpClient { BaseAddress = emulator.BaseUrl };
        var check = emulator.AssertNoRequestsAsync(id, TimeSpan.FromSeconds(2));
        using var response = await http.PostAsync("unexpected", null);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await check);
    }
    [Test]
    public async Task Generated_client_can_create_read_delete_and_report_404()
    {
        await using var server = await SampleServer.StartAsync();
        using var http = ApiClientFactory.Create(server.BaseUrl, new NoTokenProvider(), new NullEventSink());
        ISampleClient api = new SampleClient(http);
        var health = await api.GetHealthAsync();
        Assert.That(health.Status, Is.EqualTo("ok"));
        var item = await api.CreateItemAsync(new CreateItem { Name = "generated client" });
        Assert.That((await api.GetItemAsync(item.Id)).Name, Is.EqualTo(item.Name));
        await api.DeleteItemAsync(item.Id);
        var exception = Assert.ThrowsAsync<ApiException>(async () => await api.GetItemAsync(item.Id));
        Assert.That(exception!.StatusCode, Is.EqualTo(404));
    }
    [Test]
    public async Task Admin_settings_and_benchmarks_are_cleaned_per_scope()
    {
        await using var server = await SampleServer.StartAsync();
        using var http = new HttpClient { BaseAddress = server.BaseUrl };
        var admin = new AdminEmulatorClient(http);
        var scope = new CleanupScope();
        var name = Guid.NewGuid().ToString("N");
        await admin.SetOwnedTransactionAsync(name, new { delayMs = 20, success = false }, scope);
        await admin.CreateOwnedBenchmarkAsync(new Benchmark(Guid.NewGuid(), new() { ["region"] = "test" }, JsonSerializer.SerializeToElement(new { value = 10 })), scope);
        Assert.That((await admin.GetTransactionAsync(name)).GetProperty("success").GetBoolean(), Is.False);
        Assert.That(await admin.GetBenchmarksAsync(new Dictionary<string, string> { ["region"] = "test" }), Has.Length.EqualTo(1));
        await scope.DisposeAsync();
        Assert.That(await admin.GetBenchmarksAsync(), Is.Empty);
        Assert.ThrowsAsync<ApiFailureException>(async () => await admin.GetTransactionAsync(name));
    }
}
