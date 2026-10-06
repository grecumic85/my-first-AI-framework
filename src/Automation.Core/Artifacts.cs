using System.Text.Json;
namespace Automation.Core;

public interface IEventSink { void Write(string eventName, object data); }
public sealed class JsonEventSink(string directory) : IEventSink
{
    private readonly object gate = new();
    public void Write(string eventName, object data)
    {
        lock (gate)
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "events.jsonl"),
                JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, eventName, data }) + Environment.NewLine);
        }
    }
}
public sealed class NullEventSink : IEventSink { public void Write(string eventName, object data) { } }
public sealed class ArtifactDirectory
{
    public string Path { get; }
    public ArtifactDirectory(string root, string testName)
    {
        var safe = new string(testName.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_').Take(80).ToArray());
        Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, $"{safe}-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(Path);
    }
    public string File(string name)
    {
        if (System.IO.Path.GetFileName(name) != name || name is "." or "..") throw new ArgumentException("Use a file name, not a path.");
        return System.IO.Path.Combine(Path, name);
    }
}
