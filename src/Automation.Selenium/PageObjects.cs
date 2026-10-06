using Automation.Core;
using OpenQA.Selenium;
namespace Automation.Selenium;

public abstract class BasePage(IWebDriver driver, BrowserWait wait, Uri baseUrl)
{
    protected IWebDriver Driver { get; } = driver;
    protected BrowserWait Wait { get; } = wait;
    protected abstract string Route { get; }
    protected abstract By ReadyMarker { get; }
    public virtual void Open()
    {
        Driver.Navigate().GoToUrl(new Uri(baseUrl, Route));
        Wait.DocumentReady();
        Wait.Visible(ReadyMarker);
        Wait.Url(uri => uri.AbsolutePath.TrimEnd('/') == new Uri(baseUrl, Route).AbsolutePath.TrimEnd('/'));
    }
}

// A component stores locators, not cached IWebElements. The root is reacquired on every operation.
public sealed class PageComponent(BrowserWait wait, By root)
{
    public IWebElement Find(By child) => wait.Until(d => d.FindElement(root).FindElement(child), $"{child} inside {root}");
    public string Text => wait.Visible(root).Text;
}

public static class BrowserDiagnostics
{
    public static void Capture(IWebDriver driver, ArtifactDirectory artifacts, IEventSink events)
    {
        // Artifact errors must not hide the original assertion failure.
        Try("screenshot", () => ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(artifacts.File("failure.png")));
        Try("page-source", () => File.WriteAllText(artifacts.File("failure.html"), driver.PageSource));
        void Try(string name, Action capture)
        {
            try { capture(); }
            catch (Exception ex) { events.Write("artifact.failure", new { name, exceptionType = ex.GetType().Name }); }
        }
    }
}
