namespace Automation.Core;

public sealed class TestData(int seed)
{
    private readonly Random random = new(seed);
    public int Seed { get; } = seed;
    public string UniqueName(string prefix = "test") => $"{prefix}-{Guid.NewGuid():N}";
    public int Number(int min, int max) => random.Next(min, max);
    public T Pick<T>(IReadOnlyList<T> values) => values.Count > 0 ? values[random.Next(values.Count)] : throw new ArgumentException("Empty values.");
}

// Compose API actions into named, cancellable setup flows; register compensation as resources are created.
public sealed class DataFlow
{
    private readonly List<(string Name, Func<CleanupScope, CancellationToken, Task> Step)> steps = new();
    public DataFlow Add(string name, Func<CleanupScope, CancellationToken, Task> step) { steps.Add((name, step)); return this; }
    public async Task RunAsync(CleanupScope cleanup, IEventSink events, CancellationToken token = default)
    {
        foreach (var step in steps)
        {
            token.ThrowIfCancellationRequested();
            events.Write("data.step.started", new { step.Name });
            await step.Step(cleanup, token);
            events.Write("data.step.finished", new { step.Name });
        }
    }
}
