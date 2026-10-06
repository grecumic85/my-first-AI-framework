using Automation.Core;
using NUnit.Framework;
namespace Automation.TestExamples;

[TestFixture]
public sealed class CoreTests
{
    [Test]
    public async Task Poll_retries_only_explicit_transient_failures()
    {
        var calls = 0;
        var result = await Poll.UntilAsync<int>(_ => ++calls < 3 ? throw new IOException() : Task.FromResult(calls),
            n => n == 3, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(5), "eventual value", transient: e => e is IOException);
        Assert.That(result, Is.EqualTo(3));
    }
    [Test]
    public void Poll_propagates_programming_errors()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await Poll.UntilAsync<int>(_ => throw new ArgumentException(),
            _ => false, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(5), "invalid"));
    }
    [Test]
    public void Poll_has_a_real_deadline()
    {
        Assert.ThrowsAsync<TimeoutException>(async () => await Poll.UntilAsync(_ => Task.FromResult(false), x => x,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(5), "missing value"));
    }
    [Test]
    public void Poll_preserves_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await Poll.UntilAsync(_ => Task.FromResult(false), x => x,
            TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(5), "cancelled", cancellation.Token));
    }
    [Test]
    public async Task Cleanup_runs_in_reverse_order_and_continues_after_failure()
    {
        var scope = new CleanupScope();
        var order = new List<int>();
        scope.Register("parent", _ => { order.Add(1); return Task.CompletedTask; });
        scope.Register("child", _ => { order.Add(2); throw new InvalidOperationException("expected"); });
        Assert.ThrowsAsync<AggregateException>(async () => await scope.DisposeAsync());
        Assert.That(order, Is.EqualTo(new[] { 2, 1 }));
        Assert.ThrowsAsync<AggregateException>(async () => await scope.DisposeAsync());
        Assert.That(order.Count, Is.EqualTo(2));
        await Task.CompletedTask;
    }
    [Test]
    public async Task Cleanup_isolates_concurrent_scopes()
    {
        var calls = 0;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var scope = new CleanupScope();
            scope.Register("owned", _ => { Interlocked.Increment(ref calls); return Task.CompletedTask; });
        }));
        Assert.That(calls, Is.EqualTo(20));
    }
    [Test]
    public void Settings_reject_invalid_browser_and_waits()
    {
        var settings = new AutomationSettings();
        settings.Browser.Name = "invalid";
        Assert.Throws<ArgumentException>(settings.Validate);
        settings.Browser.Name = "chrome";
        settings.Wait.TimeoutSeconds = 0;
        Assert.Throws<ArgumentException>(settings.Validate);
    }
    [Test]
    public void Settings_load_json_layers_and_environment_override()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "settings.json"), "{\"Browser\":{\"Headless\":true}}");
        File.WriteAllText(Path.Combine(root, "settings.local.json"), "{\"Browser\":{\"Headless\":false}}");
        var old = Environment.GetEnvironmentVariable("AUTOMATION_Browser__Headless");
        var oldEnv = Environment.GetEnvironmentVariable("AUTOMATION_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("AUTOMATION_ENVIRONMENT", "local");
            Environment.SetEnvironmentVariable("AUTOMATION_Browser__Headless", null);
            Assert.That(AutomationSettings.Load(root).Browser.Headless, Is.False);
            Environment.SetEnvironmentVariable("AUTOMATION_Browser__Headless", "true");
            Assert.That(AutomationSettings.Load(root).Browser.Headless, Is.True);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTOMATION_Browser__Headless", old);
            Environment.SetEnvironmentVariable("AUTOMATION_ENVIRONMENT", oldEnv);
        }
    }
    [Test]
    public async Task Data_flow_registers_compensation_before_later_failure()
    {
        var cleaned = false;
        var scope = new CleanupScope();
        var flow = new DataFlow().Add("create", (c, _) =>
        { c.Register("remove", _ => { cleaned = true; return Task.CompletedTask; }); return Task.CompletedTask; })
        .Add("later failure", (_, _) => throw new InvalidOperationException());
        Assert.ThrowsAsync<InvalidOperationException>(async () => await flow.RunAsync(scope, new NullEventSink()));
        await scope.DisposeAsync();
        Assert.That(cleaned, Is.True);
    }
}
