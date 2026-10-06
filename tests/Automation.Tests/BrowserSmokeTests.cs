using Automation.Core;
using Automation.Selenium;
using Automation.Testing;
using NUnit.Framework;
using OpenQA.Selenium;
namespace Automation.TestExamples;

[TestFixture, Category("UI"), FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class BrowserSmokeTests
{
    private SampleServer server = null!;
    private TestScope scope = null!;
    [SetUp]
    public async Task Setup()
    {
        if (Environment.GetEnvironmentVariable("AUTOMATION_RUN_UI") != "1") Assert.Ignore("Set AUTOMATION_RUN_UI=1 to run local browser examples.");
        server = await SampleServer.StartAsync();
        var settings = AutomationSettings.Load(TestContext.CurrentContext.TestDirectory);
        settings.BaseUrl = server.BaseUrl.ToString();
        settings.ApiBaseUrl = server.BaseUrl.ToString();
        scope = new TestScope(settings, TestContext.CurrentContext.Test.Name);
    }
    [TearDown]
    public async Task Teardown()
    {
        try
        {
            if (scope is not null) await scope.CompleteAsync(TestContext.CurrentContext.Result.FailCount == 0 ? "Passed" : "Failed");
        }
        finally { if (server is not null) await server.DisposeAsync(); }
    }
    [Test]
    public void Can_create_item_through_page_objects_and_wait_for_rendering()
    {
        var driver = scope.Browsers.Current;
        var wait = new BrowserWait(driver, scope.Settings.Wait);
        var page = new ItemsPage(driver, wait, server.BaseUrl);
        page.Open();
        NetworkTracker.Install(driver);
        var name = scope.Data.UniqueName();
        page.Add(name);
        wait.NetworkIdle();
        wait.Gone(By.CssSelector("[data-testid=loading]"));
        Assert.That(wait.StableList(By.CssSelector("[data-testid=item]"), requireText: true).Select(e => e.Text), Does.Contain(name));
    }
    [Test]
    public void Additional_browser_session_has_isolated_storage()
    {
        var first = scope.Browsers.Current;
        first.Navigate().GoToUrl(server.BaseUrl);
        ((IJavaScriptExecutor)first).ExecuteScript("localStorage.setItem('session-test','first');");
        var second = scope.Browsers.Start();
        second.Navigate().GoToUrl(server.BaseUrl);
        Assert.That(((IJavaScriptExecutor)second).ExecuteScript("return localStorage.getItem('session-test');"), Is.Null);
        scope.Browsers.SwitchTo(0);
        Assert.That(((IJavaScriptExecutor)scope.Browsers.Current).ExecuteScript("return localStorage.getItem('session-test');"), Is.EqualTo("first"));
    }
    [Test]
    public async Task Can_download_and_read_a_completed_file()
    {
        if (!string.IsNullOrWhiteSpace(scope.Settings.Browser.GridUrl)) Assert.Ignore("This example validates local downloads; use Downloads.FetchRemote for Grid.");
        var driver = scope.Browsers.Current;
        driver.Navigate().GoToUrl(server.BaseUrl);
        new BrowserWait(driver, scope.Settings.Wait).Click(By.CssSelector("[data-testid=download]"));
        var directory = Directory.GetDirectories(Path.Combine(scope.Artifacts.Path, "downloads")).Single();
        var file = await Downloads.WaitForFileAsync(directory, "sample.csv", TimeSpan.FromSeconds(15));
        Assert.That(await File.ReadAllTextAsync(file), Does.Contain("1,Sample"));
    }
}
public sealed class ItemsPage(IWebDriver driver, BrowserWait wait, Uri baseUrl) : BasePage(driver, wait, baseUrl)
{
    protected override string Route => "";
    protected override By ReadyMarker => By.CssSelector("[data-testid=page-title]");
    public void Add(string name)
    {
        var input = Wait.Visible(By.CssSelector("[data-testid=item-name]"));
        input.Clear(); input.SendKeys(name);
        Wait.Click(By.CssSelector("[data-testid=add-item]"));
    }
}
