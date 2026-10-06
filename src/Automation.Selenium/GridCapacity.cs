using System.Text.Json;
using Automation.Core;
namespace Automation.Selenium;

public sealed class GridCapacity(HttpClient http)
{
    public async Task<bool> HasFreeSlotAsync(string browser, CancellationToken token = default)
    {
        using var response = await http.GetAsync("status", token);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
        var value = document.RootElement.GetProperty("value");
        if (!value.GetProperty("ready").GetBoolean()) return false;
        if (!value.TryGetProperty("nodes", out var nodes)) return false;
        return nodes.EnumerateArray().Any(n => n.GetProperty("availability").GetString() == "UP" &&
            n.GetProperty("slots").EnumerateArray().Any(s => s.GetProperty("session").ValueKind == JsonValueKind.Null &&
                string.Equals(s.GetProperty("stereotype").GetProperty("browserName").GetString(), browser, StringComparison.OrdinalIgnoreCase)));
    }
    // A probe reduces pressure; it is not a reservation. Grid's queue arbitrates concurrent session creation.
    public Task<bool> WaitForSlotAsync(string browser, TimeSpan timeout, CancellationToken token = default)
        => Poll.UntilAsync(ct => HasFreeSlotAsync(browser, ct), available => available, timeout,
            TimeSpan.FromSeconds(1), "Available Selenium Grid slot", token);
}
