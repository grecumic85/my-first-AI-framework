namespace Automation.Core;

public sealed class CleanupScope : IAsyncDisposable
{
    private readonly Stack<(string Name, Func<CancellationToken, Task> Action)> actions = new();
    private readonly object gate = new();
    private Task? disposal;
    public void Register(string name, Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (gate)
        {
            if (disposal is not null) throw new ObjectDisposedException(nameof(CleanupScope));
            actions.Push((name, action));
        }
    }
    public ValueTask DisposeAsync()
    {
        lock (gate) return new ValueTask(disposal ??= RunAsync(actions.ToArray()));
    }
    private static async Task RunAsync((string Name, Func<CancellationToken, Task> Action)[] work)
    {
        // Yield prevents cleanup callbacks from running under the registration lock.
        await Task.Yield();
        var failures = new List<Exception>();
        foreach (var item in work)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await item.Action(timeout.Token).WaitAsync(timeout.Token).ConfigureAwait(false); }
            catch (Exception ex) { failures.Add(new InvalidOperationException($"Cleanup failed: {item.Name}", ex)); }
        }
        if (failures.Count > 0) throw new AggregateException(failures);
    }
}
