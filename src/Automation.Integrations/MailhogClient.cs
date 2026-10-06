using System.Net.Http.Json;
using System.Text.Json;
using Automation.Core;
namespace Automation.Integrations;

public sealed record EmailMessage(string Id, string Subject, string Body);
public interface IEmailInbox
{
    Task<EmailMessage> WaitAsync(string recipient, string subjectContains, TimeSpan timeout, CancellationToken token = default);
}
public sealed class MailhogClient(HttpClient http) : IEmailInbox
{
    public async Task<IReadOnlyList<EmailMessage>> SearchAsync(string recipient, CancellationToken token = default)
    {
        using var response = await http.GetAsync($"api/v2/search?kind=to&query={Uri.EscapeDataString(recipient)}&limit=100", token);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
        return json.RootElement.GetProperty("items").EnumerateArray().Select(message =>
        {
            var content = message.GetProperty("Content");
            var subject = content.GetProperty("Headers").TryGetProperty("Subject", out var value) ? value[0].GetString() ?? "" : "";
            return new EmailMessage(message.GetProperty("ID").GetString()!, subject, content.GetProperty("Body").GetString() ?? "");
        }).ToArray();
    }
    // Use a unique recipient for each test to avoid matching old messages.
    public Task<EmailMessage> WaitAsync(string recipient, string subjectContains, TimeSpan timeout, CancellationToken token = default)
        => Poll.UntilAsync(async ct => (await SearchAsync(recipient, ct)).FirstOrDefault(m => m.Subject.Contains(subjectContains, StringComparison.Ordinal)),
            email => email is not null, timeout, TimeSpan.FromMilliseconds(250), "Matching email", token)!;
    public async Task DeleteAsync(string id, CancellationToken token = default)
    {
        using var response = await http.DeleteAsync($"api/v1/messages/{Uri.EscapeDataString(id)}", token);
        response.EnsureSuccessStatusCode();
    }
}
