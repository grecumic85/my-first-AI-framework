using System.Text.Json;
using Automation.Api;
using Automation.Core;
namespace Automation.Emulation;

public sealed record Benchmark(Guid Id, Dictionary<string, string> Match, JsonElement Response);
public interface IEmulatorAdmin
{
    Task SetTransactionAsync(string name, object setting, CancellationToken token = default);
    Task<JsonElement> GetTransactionAsync(string name, CancellationToken token = default);
    Task DeleteTransactionAsync(string name, CancellationToken token = default);
    Task<Benchmark> CreateBenchmarkAsync(Benchmark benchmark, CancellationToken token = default);
    Task DeleteBenchmarkAsync(Guid id, CancellationToken token = default);
}

// Generic version of the source admin/transaction-settings and admin/benchmarks contract.
// Adapt this adapter and DTOs to your emulator's actual contract; the sample app implements this one.
public sealed class AdminEmulatorClient(HttpClient http) : IEmulatorAdmin
{
    private readonly JsonApiClient api = new(http);
    public Task SetTransactionAsync(string name, object setting, CancellationToken token = default)
        => api.SendAsync(HttpMethod.Post, $"admin/transaction-settings/{Uri.EscapeDataString(name)}", setting, token);
    public Task<JsonElement> GetTransactionAsync(string name, CancellationToken token = default)
        => api.GetAsync<JsonElement>($"admin/transaction-settings/{Uri.EscapeDataString(name)}", token);
    public Task DeleteTransactionAsync(string name, CancellationToken token = default)
        => api.SendAsync(HttpMethod.Delete, $"admin/transaction-settings/{Uri.EscapeDataString(name)}", token: token);
    public Task<Benchmark> CreateBenchmarkAsync(Benchmark benchmark, CancellationToken token = default)
        => api.PostAsync<Benchmark, Benchmark>("admin/benchmarks", benchmark, token);
    public Task<Benchmark[]> GetBenchmarksAsync(IReadOnlyDictionary<string, string>? match = null, CancellationToken token = default)
    {
        var query = match is null ? "" : "?" + string.Join("&", match.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        return api.GetAsync<Benchmark[]>("admin/benchmarks" + query, token);
    }
    public Task DeleteBenchmarkAsync(Guid id, CancellationToken token = default)
        => api.SendAsync(HttpMethod.Delete, $"admin/benchmarks/{id}", token: token);
    public async Task CreateOwnedBenchmarkAsync(Benchmark value, CleanupScope cleanup, CancellationToken token = default)
    {
        var created = await CreateBenchmarkAsync(value, token);
        cleanup.Register($"benchmark {created.Id}", ct => DeleteBenchmarkAsync(created.Id, ct));
    }
    public async Task SetOwnedTransactionAsync(string uniqueName, object setting, CleanupScope cleanup, CancellationToken token = default)
    {
        await SetTransactionAsync(uniqueName, setting, token);
        cleanup.Register($"transaction setting {uniqueName}", ct => DeleteTransactionAsync(uniqueName, ct));
    }
}
