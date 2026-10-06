using Automation.Core;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
namespace Automation.Selenium;

public interface IBrowserFactory { IWebDriver Create(string downloadDirectory); }
public sealed class BrowserFactory(BrowserSettings settings) : IBrowserFactory
{
    public IWebDriver Create(string downloadDirectory)
    {
        Directory.CreateDirectory(downloadDirectory);
        DriverOptions options;
        switch (settings.Name.ToLowerInvariant())
        {
            case "chrome":
                var chrome = new ChromeOptions();
                if (settings.Headless) chrome.AddArgument("--headless=new");
                chrome.AddArgument("--window-size=1440,1000");
                chrome.AddUserProfilePreference("download.default_directory", Path.GetFullPath(downloadDirectory));
                chrome.AddUserProfilePreference("download.prompt_for_download", false);
                options = chrome;
                break;
            case "edge":
                var edge = new EdgeOptions();
                if (settings.Headless) edge.AddArgument("--headless=new");
                edge.AddArgument("--window-size=1440,1000");
                edge.AddUserProfilePreference("download.default_directory", Path.GetFullPath(downloadDirectory));
                edge.AddUserProfilePreference("download.prompt_for_download", false);
                options = edge;
                break;
            case "firefox":
                var firefox = new FirefoxOptions();
                if (settings.Headless) firefox.AddArgument("-headless");
                firefox.SetPreference("browser.download.folderList", 2);
                firefox.SetPreference("browser.download.dir", Path.GetFullPath(downloadDirectory));
                firefox.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/pdf,application/zip,text/csv,application/octet-stream");
                options = firefox;
                break;
            default: throw new ArgumentException("Unsupported browser.");
        }
        var remote = !string.IsNullOrWhiteSpace(settings.GridUrl);
        if (remote) options.AddAdditionalOption("se:downloadsEnabled", true);
        IWebDriver driver = remote
            ? new RemoteWebDriver(new Uri(settings.GridUrl!), options.ToCapabilities(), TimeSpan.FromSeconds(settings.CommandTimeoutSeconds)) { FileDetector = new LocalFileDetector() }
            : options switch
            {
                ChromeOptions c => new ChromeDriver(ChromeDriverService.CreateDefaultService(), c, TimeSpan.FromSeconds(settings.CommandTimeoutSeconds)),
                EdgeOptions e => new EdgeDriver(EdgeDriverService.CreateDefaultService(), e, TimeSpan.FromSeconds(settings.CommandTimeoutSeconds)),
                FirefoxOptions f => new FirefoxDriver(FirefoxDriverService.CreateDefaultService(), f, TimeSpan.FromSeconds(settings.CommandTimeoutSeconds)),
                _ => throw new InvalidOperationException()
            };
        try
        {
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(settings.CommandTimeoutSeconds);
            driver.Manage().Window.Size = new System.Drawing.Size(1440, 1000);
            return driver;
        }
        catch { driver.Dispose(); throw; }
    }
}

// One instance per test/scenario. Never share an instance across concurrently executing tests.
public sealed class BrowserSessions(IBrowserFactory factory, string downloadsRoot) : IDisposable
{
    private readonly List<IWebDriver> sessions = new();
    private int current;
    private bool disposed;
    public bool Started => sessions.Count > 0;
    public int Count => sessions.Count;
    public IWebDriver Current => Started ? sessions[current] : Start();
    public IWebDriver Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var driver = factory.Create(Path.Combine(downloadsRoot, Guid.NewGuid().ToString("N")));
        sessions.Add(driver);
        current = sessions.Count - 1;
        return driver;
    }
    public void SwitchTo(int index)
    {
        if (index < 0 || index >= sessions.Count) throw new ArgumentOutOfRangeException(nameof(index));
        current = index;
    }
    public void CloseCurrent()
    {
        if (!Started) return;
        var driver = sessions[current];
        sessions.RemoveAt(current);
        current = 0;
        driver.Dispose();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        var failures = new List<Exception>();
        foreach (var driver in sessions) try { driver.Dispose(); } catch (Exception ex) { failures.Add(ex); }
        sessions.Clear();
        if (failures.Count > 0) throw new AggregateException("Browser cleanup failed.", failures);
    }
}
