using System.Net.Http.Json;
using System.Text.Json;
namespace Automation.Integrations;

public sealed record ZephyrSettings(string ReleaseId, string CyclePhaseId, string TesterId,
    int PassedStatus = 1, int FailedStatus = 11, int SkippedStatus = 4);

// Adapter for the Enterprise contract used in the source; not Zephyr Scale/Squad.
public sealed class ZephyrEnterprisePublisher(HttpClient http, ZephyrSettings settings) : ITestResultPublisher
{
    public async Task PublishAsync(TestResult result, CancellationToken token = default)
    {
        foreach (var caseId in result.CaseIds)
        {
            string? execution = null;
            for (var offset = 0; offset < 10000 && execution is null; offset += 50)
            {
                var endpoint = "advancesearch?word=" + Uri.EscapeDataString("testcaseId=" + caseId) +
                    "&entitytype=execution&releaseid=" + Uri.EscapeDataString(settings.ReleaseId) +
                    $"&zql=true&order=id&isasorder=false&firstresult={offset}&maxresults=50";
                using var response = await http.GetAsync(endpoint, token);
                response.EnsureSuccessStatusCode();
                using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
                var rows = json.RootElement[0].GetProperty("results");
                foreach (var row in rows.EnumerateArray())
                    if (row.GetProperty("cyclePhaseId").ToString() == settings.CyclePhaseId) execution = row.GetProperty("id").ToString();
                if (rows.GetArrayLength() < 50) break;
            }
            if (execution is null) throw new InvalidOperationException("Zephyr execution was not found in the configured release/cycle phase.");
            var status = result.Outcome == "Passed" ? settings.PassedStatus : result.Outcome == "Skipped" ? settings.SkippedStatus : settings.FailedStatus;
            using var update = await http.PostAsJsonAsync("execution/updateteststatus", new[] { new
            { releaseTestScheduleId = execution, executionNotes = result.TestName, executionStatus = status.ToString(), testerId = settings.TesterId } }, token);
            update.EnsureSuccessStatusCode();
        }
    }
}
public sealed class JiraIssueClient(HttpClient http)
{
    // Returns issue metadata for reporting. Does not silently change assertion results.
    public async Task<string> GetStatusAsync(string issueKey, CancellationToken token = default)
    {
        using var response = await http.GetAsync($"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}?fields=status", token);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
        return json.RootElement.GetProperty("fields").GetProperty("status").GetProperty("name").GetString()!;
    }
}
