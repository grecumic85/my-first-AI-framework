namespace Automation.Integrations;
public sealed class SchedulerClient(HttpClient http)
{
    // Matches the source recurring-job form protocol. Configure base URL/auth for your scheduler.
    public async Task TriggerAsync(string job, CancellationToken token = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["jobs[]"] = job });
        using var response = await http.PostAsync("dashboard/recurring/trigger", content, token);
        response.EnsureSuccessStatusCode();
    }
}
