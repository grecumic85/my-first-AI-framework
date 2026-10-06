using Automation.Core;
using Automation.Selenium;
using Moq;
using NUnit.Framework;
using OpenQA.Selenium;
namespace Automation.TestExamples;

[TestFixture]
public sealed class SeleniumWaitTests
{
    private static WaitSettings Fast => new() { TimeoutSeconds = 0.15, PollMilliseconds = 5, StableMilliseconds = 15 };
    [Test]
    public void Visibility_relocates_after_stale_DOM_replacement()
    {
        var driver = new Mock<IWebDriver>();
        var element = new Mock<IWebElement>();
        element.SetupGet(e => e.Displayed).Returns(true);
        var locator = By.Id("dynamic");
        driver.SetupSequence(d => d.FindElement(locator)).Throws<StaleElementReferenceException>().Returns(element.Object);
        Assert.That(new BrowserWait(driver.Object, Fast).Visible(locator), Is.SameAs(element.Object));
        driver.Verify(d => d.FindElement(locator), Times.Exactly(2));
    }
    [Test]
    public void Stable_list_does_not_accept_an_empty_list_by_default()
    {
        var driver = new Mock<IWebDriver>();
        var locator = By.TagName("li");
        driver.Setup(d => d.FindElements(locator)).Returns(new System.Collections.ObjectModel.ReadOnlyCollection<IWebElement>([]));
        Assert.Throws<WebDriverTimeoutException>(() => new BrowserWait(driver.Object, Fast).StableList(locator));
    }
    [Test]
    public void Clickable_waits_for_overlay_to_clear_and_geometry_to_stabilize()
    {
        var driver = new Mock<IWebDriver>();
        var js = driver.As<IJavaScriptExecutor>();
        var element = new Mock<IWebElement>();
        element.SetupGet(e => e.Displayed).Returns(true);
        element.SetupGet(e => e.Enabled).Returns(true);
        var locator = By.Id("button");
        driver.Setup(d => d.FindElement(locator)).Returns(element.Object);
        var calls = 0;
        js.Setup(j => j.ExecuteScript(It.IsAny<string>(), It.IsAny<object[]>())).Returns(() => ++calls < 3 ? "" : "[1,2,30,40]");
        Assert.That(new BrowserWait(driver.Object, Fast).Clickable(locator), Is.SameAs(element.Object));
        Assert.That(calls, Is.GreaterThanOrEqualTo(4));
    }
    [Test]
    public void Non_transient_driver_errors_fail_immediately()
    {
        var driver = new Mock<IWebDriver>();
        var locator = By.Id("broken");
        driver.Setup(d => d.FindElement(locator)).Throws(new InvalidSelectorException());
        Assert.Throws<InvalidSelectorException>(() => new BrowserWait(driver.Object, Fast).Present(locator));
        driver.Verify(d => d.FindElement(locator), Times.Once);
    }
    [Test]
    public void Network_wait_requires_instrumentation()
    {
        var driver = new Mock<IWebDriver>();
        driver.As<IJavaScriptExecutor>().Setup(j => j.ExecuteScript(It.IsAny<string>(), It.IsAny<object[]>())).Returns((object?)null);
        Assert.Throws<InvalidOperationException>(() => new BrowserWait(driver.Object, Fast).NetworkIdle());
    }
}
