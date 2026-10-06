using System.IO.Compression;
using Automation.Core;
using OpenQA.Selenium;
namespace Automation.Selenium;

public static class Downloads
{
    public static Task<string> WaitForFileAsync(string directory, string name, TimeSpan timeout, CancellationToken token = default)
    {
        if (Path.GetFileName(name) != name) throw new ArgumentException("Use a file name.");
        var path = Path.Combine(directory, name);
        long previous = -1;
        var stable = System.Diagnostics.Stopwatch.StartNew();
        return Poll.UntilAsync(_ =>
        {
            if (!File.Exists(path) || File.Exists(path + ".crdownload") || File.Exists(path + ".part"))
            { previous = -1; stable.Restart(); return Task.FromResult(false); }
            try
            {
                using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                if (file.Length != previous) { previous = file.Length; stable.Restart(); return Task.FromResult(false); }
                return Task.FromResult(stable.ElapsedMilliseconds >= 300);
            }
            catch (IOException) { stable.Restart(); return Task.FromResult(false); }
        }, ready => ready, timeout, TimeSpan.FromMilliseconds(100), $"Completed download {name}", token)
            .ContinueWith(t => { t.GetAwaiter().GetResult(); return path; }, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
    public static IReadOnlyList<string> ZipEntries(string file)
    { using var zip = ZipFile.OpenRead(file); return zip.Entries.Select(e => e.FullName).ToArray(); }
    // Selenium Grid managed downloads must be enabled on the node (see compose.yml).
    public static void FetchRemote(IWebDriver driver, string fileName, string destination)
    {
        if (driver is not IHasDownloads downloads) throw new NotSupportedException("Driver does not support managed downloads.");
        downloads.DownloadFile(fileName, destination);
    }
}
