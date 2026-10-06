using System.Diagnostics;
using System.Text.Json;
using Automation.Core;
using Automation.Testing;
using DatePickerScenarios;

var allowed = new[] { "--list", "--live", "--self-test", "--site", "--case", "--headful", "--output" };
for (var i = 0; i < args.Length; i++)
{
    if (!allowed.Contains(args[i])) throw new ArgumentException($"Unknown option {args[i]}");
    if (args[i] is "--site" or "--case" or "--output")
        if (++i >= args.Length || args[i].StartsWith("--")) throw new ArgumentException("Option needs a value.");
}
string? Option(string name) { var index = Array.IndexOf(args, name); return index < 0 ? null : args[index + 1]; }
if (args.Contains("--list"))
{
    foreach (var s in Scenarios.All) Console.WriteLine($"{s.Id} [{s.Site}] {s.Description}");
    return 0;
}
if (args.Contains("--self-test")) return NavigationChecks.Run();
if (!args.Contains("--live"))
{
    Console.WriteLine("Use --self-test for local checks; --list for cases; --live [--site lastest|globalsqa|practice|uk] [--case LAT-01] [--headful] [--output artifacts/datepickers].");
    return 0;
}
var cases = Scenarios.All.Where(s => Option("--site") is not { } site || s.Site.Equals(site, StringComparison.OrdinalIgnoreCase))
    .Where(s => Option("--case") is not { } id || s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).ToArray();
if (cases.Length == 0) throw new ArgumentException("No matching cases; use --list.");
var output = Path.GetFullPath(Option("--output") ?? "artifacts/datepickers");
Directory.CreateDirectory(output);
var results = new List<object>();
var failures = 0;
foreach (var test in cases)
{
    Console.WriteLine($"RUN {test.Id}: {test.Description}");
    var settings = AutomationSettings.Load(Directory.GetCurrentDirectory());
    settings.ArtifactsPath = output;
    if (args.Contains("--headful")) settings.Browser.Headless = false;
    // Each case gets an independent session, cookies, download directory, and cleanup scope.
    var watch = Stopwatch.StartNew();
    TestScope? scope = null; Exception? failure = null;
    try { scope = new TestScope(settings, test.Id); test.Execute(scope); }
    catch (Exception ex) { failure = ex; }
    finally
    {
        if (scope is not null)
            try { await scope.CompleteAsync(failure is null ? "Passed" : "Failed"); }
            catch (Exception ex) { failure = failure is null ? ex : new AggregateException(failure, ex); }
    }
    if (failure is not null) failures++;
    var outcome = failure is null ? "Passed" : "Failed";
    Console.WriteLine($"{outcome.ToUpperInvariant()} {test.Id} ({watch.Elapsed.TotalSeconds:F1}s){(failure is null ? "" : ": " + failure.Message)}");
    results.Add(new { test.Id, test.Site, test.Description, outcome, seconds = watch.Elapsed.TotalSeconds,
        artifacts = scope is null ? null : Path.GetRelativePath(output, scope.Artifacts.Path), error = failure?.ToString() });
    // Persist after every case so interruption still leaves usable evidence.
    await File.WriteAllTextAsync(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new
    { observedAtUtc = DateTimeOffset.UtcNow, runtime = Environment.Version.ToString(), total = results.Count, failures, results }, new JsonSerializerOptions { WriteIndented = true }));
}
Console.WriteLine($"{cases.Length - failures}/{cases.Length} passed. Results: {Path.Combine(output, "results.json")}");
return failures == 0 ? 0 : 1;
