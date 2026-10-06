using System.Net.Http.Json;
using System.Text.Json;
namespace Automation.Integrations;

public sealed record TestResult(string TestName, string Outcome, double DurationSeconds, IReadOnlyList<string> CaseIds);
public interface ITestResultPublisher { Task PublishAsync(TestResult result, CancellationToken token = default); }
public sealed class JsonResultPublisher(string path) : ITestResultPublisher
{
    public Task PublishAsync(TestResult result, CancellationToken token = default)
        => File.WriteAllTextAsync(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }), token);
}
// Explicitly inject this adapter to opt into an external side effect. No publishing occurs by default.
// TestRail API v2 expects base address ending /index.php?/api/v2/ (query-style routing).
public sealed class TestRailPublisher(HttpClient http, string runId) : ITestResultPublisher
{
    public async Task PublishAsync(TestResult result, CancellationToken token = default)
    {
        foreach (var id in result.CaseIds)
        {
            if (!long.TryParse(id, out _) || !long.TryParse(runId, out _)) throw new ArgumentException("TestRail IDs must be numeric.");
            var endpoint = $"{http.BaseAddress!.OriginalString.TrimEnd('/')}/add_result_for_case/{runId}/{id}";
            using var response = await http.PostAsJsonAsync(endpoint, new
            { status_id = result.Outcome == "Passed" ? 1 : result.Outcome == "Skipped" ? 2 : 5, comment = result.TestName }, token);
            response.EnsureSuccessStatusCode();
        }
    }
}
// Payload and endpoint are explicit because Zephyr product editions have different contracts.
public sealed class JsonWebhookPublisher(HttpClient http, string relativeEndpoint) : ITestResultPublisher
{
    public async Task PublishAsync(TestResult result, CancellationToken token = default)
    {
        using var response = await http.PostAsJsonAsync(relativeEndpoint, result, token);
        response.EnsureSuccessStatusCode();
    }
}
