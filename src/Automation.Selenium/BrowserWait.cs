using System.Diagnostics;
using Automation.Core;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
namespace Automation.Selenium;

public interface IElementWait
{
    IWebElement Present(By locator);
    IWebElement Visible(By locator);
    IWebElement Clickable(By locator);
    void Gone(By locator);
    string Text(By locator, Func<string, bool> predicate);
}

public sealed class BrowserWait(IWebDriver driver, WaitSettings settings) : IElementWait
{
    public T Until<T>(Func<IWebDriver, T> condition, string message, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(settings.TimeoutSeconds))
        { PollingInterval = TimeSpan.FromMilliseconds(settings.PollMilliseconds), Message = message };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        return wait.Until(condition);
    }
    public IWebElement Present(By locator) => Until(d => d.FindElement(locator), $"Presence of {locator}");
    public IWebElement Visible(By locator) => Until(d =>
    { var e = d.FindElement(locator); return e.Displayed ? e : null!; }, $"Visibility of {locator}");
    public void Gone(By locator) => Until(d => !d.FindElements(locator).Any(e => e.Displayed), $"Disappearance of {locator}");
    public void Enabled(By locator, bool expected = true) => Until(d => d.FindElement(locator).Enabled == expected, $"Enabled={expected}: {locator}");
    public string Text(By locator, Func<string, bool> predicate)
    {
        string result = "";
        Until(d => predicate(result = d.FindElement(locator).Text), $"Text condition for {locator}");
        return result;
    }
    public IWebElement Clickable(By locator)
    {
        string? previous = null;
        var stable = Stopwatch.StartNew();
        return Until(d =>
        {
            // Re-find on EVERY poll so DOM replacement never traps us on a stale handle.
            var e = d.FindElement(locator);
            if (!e.Displayed || !e.Enabled) { previous = null; stable.Restart(); return null!; }
            var signature = (string?)((IJavaScriptExecutor)d).ExecuteScript("""
                const e=arguments[0];e.scrollIntoView({block:'center',inline:'center'});
                const r=e.getBoundingClientRect();
                if(r.width<=0||r.height<=0)return '';
                const top=document.elementFromPoint(r.x+r.width/2,r.y+r.height/2);
                if(!top || !(top===e||e.contains(top)))return '';
                return JSON.stringify([r.x,r.y,r.width,r.height]);
                """, e) ?? "";
            if (signature.Length == 0 || signature != previous)
            { previous = signature; stable.Restart(); return null!; }
            return stable.ElapsedMilliseconds >= settings.StableMilliseconds ? e : null!;
        }, $"Stable, enabled, unobscured element {locator}");
    }
    public void Click(By locator)
    {
        // Click once after waiting. Do not blindly replay a potentially successful submission.
        Clickable(locator).Click();
    }
    public IReadOnlyList<IWebElement> StableList(By locator, int minimumCount = 1, bool requireText = false)
    {
        string? previous = null;
        var stable = Stopwatch.StartNew();
        return Until(d =>
        {
            var items = d.FindElements(locator);
            var texts = items.Select(e => e.Text).ToArray();
            var signature = System.Text.Json.JsonSerializer.Serialize(texts);
            if (items.Count < minimumCount || (requireText && texts.Any(string.IsNullOrWhiteSpace)) || signature != previous)
            { previous = signature; stable.Restart(); return null!; }
            return stable.ElapsedMilliseconds >= settings.StableMilliseconds ? items.ToArray() : null!;
        }, $"Stable list {locator}, minimum {minimumCount}");
    }
    public void DocumentReady() => Until(d => Equals(((IJavaScriptExecutor)d).ExecuteScript("return document.readyState"), "complete"), "Document ready");
    public void Url(Func<Uri, bool> predicate) => Until(d => Uri.TryCreate(d.Url, UriKind.Absolute, out var uri) && predicate(uri), "Expected URL");
    public void NetworkIdle(int acceptablePendingRequests = 0)
    {
        if (acceptablePendingRequests < 0) throw new ArgumentOutOfRangeException(nameof(acceptablePendingRequests));
        var stable = Stopwatch.StartNew();
        Until(d =>
        {
            var result = ((IJavaScriptExecutor)d).ExecuteScript("return window.__automationPendingRequests ?? null;");
            if (result is null) throw new InvalidOperationException("Install NetworkTracker before initiating the requests being observed.");
            if (Convert.ToInt64(result) > acceptablePendingRequests) { stable.Restart(); return false; }
            return stable.ElapsedMilliseconds >= settings.StableMilliseconds;
        }, "Network idle");
    }
}
