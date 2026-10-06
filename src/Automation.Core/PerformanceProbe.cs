using System.Diagnostics;
namespace Automation.Core;
public sealed record LatencySummary(int Samples, double MeanMs, double P95Ms, double MaximumMs);
public static class PerformanceProbe
{
    // Sequential timing check, not a concurrent load generator. Warm-up is excluded from statistics.
    public static async Task<LatencySummary> MeasureAsync(Func<CancellationToken, Task> operation,
        int samples = 10, int warmup = 1, CancellationToken token = default)
    {
        if (samples < 1 || warmup < 0) throw new ArgumentOutOfRangeException(nameof(samples));
        for (var i = 0; i < warmup; i++) { token.ThrowIfCancellationRequested(); await operation(token); }
        var times = new double[samples];
        for (var i = 0; i < samples; i++)
        {
            token.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            await operation(token);
            times[i] = watch.Elapsed.TotalMilliseconds;
        }
        Array.Sort(times);
        return new LatencySummary(samples, times.Average(), times[(int)Math.Ceiling(samples * .95) - 1], times[^1]);
    }
}
