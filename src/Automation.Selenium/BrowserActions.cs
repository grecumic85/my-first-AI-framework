using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
namespace Automation.Selenium;

public static class BrowserActions
{
    public static void Upload(BrowserWait wait, By fileInput, string file)
    {
        var path = Path.GetFullPath(file);
        if (!File.Exists(path)) throw new FileNotFoundException("Upload file not found.", path);
        wait.Present(fileInput).SendKeys(path);
    }
    public static void DragAndDrop(IWebDriver driver, BrowserWait wait, By source, By target)
        => new Actions(driver).DragAndDrop(wait.Visible(source), wait.Visible(target)).Perform();
    public static void SwitchToNewWindow(IWebDriver driver, BrowserWait wait, IReadOnlyCollection<string> previous)
    {
        var handle = wait.Until(d => d.WindowHandles.Except(previous).FirstOrDefault()!, "New browser window");
        driver.SwitchTo().Window(handle);
    }
    public static void SwitchToFrame(IWebDriver driver, BrowserWait wait, By frame) => driver.SwitchTo().Frame(wait.Present(frame));
}
