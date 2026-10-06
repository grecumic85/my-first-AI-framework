using System.Diagnostics;
namespace Automation.Core;

public static class Poll
{
    // Cancellation applies to both the callback and delay. Callbacks must honor their token.
    public static async Task<T> UntilAsync<T>(Func<CancellationToken, Task<T>> read,
        Func<T, bool> satisfied, TimeSpan timeout, TimeSpan interval, string description,
        CancellationToken cancellationToken = default, Func<Exception, bool>? transient = null)
    {
        if (timeout <= TimeSpan.Zero || interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        Exception? lastError = null;
        var elapsed = Stopwatch.StartNew();
        try
        {
            while (true)
            {
                deadline.Token.ThrowIfCancellationRequested();
                try
                {
                    var result = await read(deadline.Token).ConfigureAwait(false);
                    deadline.Token.ThrowIfCancellationRequested();
                    if (satisfied(result)) return result;
                }
                catch (Exception ex) when (ex is not OperationCanceledException && transient?.Invoke(ex) == true)
                { lastError = ex; }
                await Task.Delay(interval, deadline.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new TimeoutException($"Timed out after {elapsed.Elapsed.TotalSeconds:F1}s: {description}", lastError); }
    }
}
