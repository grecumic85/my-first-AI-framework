using System.Globalization;
using Automation.Selenium;
using Automation.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DatePickerScenarios;

public abstract class CalendarPage
{
    protected IWebDriver Driver { get; }
    public BrowserWait Wait { get; }
    public MonthNavigator Navigator { get; } = new();
    protected CalendarPage(TestScope scope, string url)
    {
        Driver = scope.Browsers.Current;
        Wait = new BrowserWait(Driver, scope.Settings.Wait);
        Driver.Navigate().GoToUrl(url);
    }
    protected static By Css(string selector) => By.CssSelector(selector);
    protected static By TestId(string id) => Css($"[data-testid='{id}']");
    protected string Text(string selector) => Wait.Visible(Css(selector)).Text.Trim();
    protected DateOnly ParseMonth(string month, string year) => DateOnly.ParseExact($"{month.Trim()} {year.Trim()}", new[] { "MMMM yyyy", "MMM yyyy" }, CalendarDates.English);
    public void ExpectValue(By input, string expected) => Wait.Until(d => d.FindElement(input).GetDomProperty("value") == expected,
        $"Expected {input} value '{expected}'");
    public string Value(By input) => Wait.Present(input).GetDomProperty("value") ?? "";
    public virtual void Dismiss() => Driver.FindElement(By.TagName("body")).SendKeys(Keys.Escape);
}

public sealed class LastestPage(TestScope scope) : CalendarPage(scope, Url)
{
    public const string Url = "https://lastest.cloud/playground/date-picker";
    public IMonthCalendar Calendar(bool range = false)
    {
        var prefix = range ? "range-calendar" : "calendar";
        Wait.Visible(TestId(prefix));
        return new DomMonthCalendar(Wait,
            () => DateOnly.ParseExact(Wait.Visible(TestId(prefix + "-month")).Text, "MMMM yyyy", CalendarDates.English),
            TestId(prefix + "-prev"), TestId(prefix + "-next"), d => TestId(prefix + "-day-" + CalendarDates.Iso(d)));
    }
    public void ExpectPicked(DateOnly date) => Wait.Text(TestId("calendar-status"), t => t ==
        $"Picked: {CalendarDates.Iso(date)} — {date.ToString("dddd, d MMMM yyyy", CalendarDates.English)}");
    public void ExpectRangeStart(DateOnly date) => Wait.Text(TestId("range-status"), t => t == $"Check-in {CalendarDates.Iso(date)} — now pick a check-out date.");
    public void ExpectRange(DateOnly start, DateOnly end) => Wait.Text(TestId("range-status"), t => t ==
        $"{end.DayNumber - start.DayNumber} nights: {CalendarDates.Iso(start)} → {CalendarDates.Iso(end)}");
    public void ResetRange() { Wait.Click(TestId("range-reset")); Wait.Text(TestId("range-status"), t => t == "Select a check-in date."); }
    public void ExpectWeekendsDisabled(DateOnly month, bool range = false)
    {
        var prefix = range ? "range-calendar" : "calendar";
        for (var i = 1; i <= DateTime.DaysInMonth(month.Year, month.Month); i++)
        {
            var date = new DateOnly(month.Year, month.Month, i);
            var element = Wait.Present(TestId(prefix + "-day-" + CalendarDates.Iso(date)));
            if (element.Enabled == CalendarDates.Weekend(date))
                throw new InvalidOperationException($"Incorrect disabled state for {CalendarDates.Iso(date)}.");
        }
    }
}

public sealed class GlobalSqaPage : CalendarPage
{
    public const string Url = "https://www.globalsqa.com/demo-site/datepicker/#Simple%20Date%20Picker";
    public static readonly By Input = By.Id("datepicker");
    public GlobalSqaPage(TestScope scope) : base(scope, Url) { EnterDemo("Simple Date Picker", "default.html"); }
    public void EnterDemo(string tab, string frameFile)
    {
        Driver.SwitchTo().DefaultContent();
        // A normal privacy choice, only when the known CMP is visible. No overlay removal or forced clicks.
        var manage = By.XPath("//button[normalize-space()='Manage options']");
        var tabLocator = By.XPath($"//*[@role='tab' and normalize-space()='{tab}']");
        Wait.Until(d => d.FindElements(manage).Any(e => e.Displayed) || d.FindElements(tabLocator).Any(e => e.Displayed), "Demo tab or consent choices");
        HandleConsent(manage);
        try { Wait.Click(tabLocator); }
        catch (WebDriverTimeoutException) when (Driver.FindElements(manage).Any(e => e.Displayed))
        { HandleConsent(manage); Wait.Click(tabLocator); }
        var frame = Wait.Visible(Css($"iframe.demo-frame[src$='/{frameFile}']"));
        Driver.SwitchTo().Frame(frame);
        Wait.Visible(Input);
    }
    private void HandleConsent(By manage)
    {
        if (!Driver.FindElements(manage).Any(e => e.Displayed)) return;
        Wait.Click(manage);
        var confirm = By.XPath("//button[normalize-space()='Confirm choices']");
        Wait.Click(confirm);
        Wait.Gone(confirm);
    }
    public IMonthCalendar Open(bool icon = false)
    {
        Wait.Click(icon ? Css(".ui-datepicker-trigger") : Input);
        Wait.Visible(By.Id("ui-datepicker-div"));
        return new DomMonthCalendar(Wait, () =>
        {
            var month = Wait.Visible(Css(".ui-datepicker-month"));
            var year = Wait.Visible(Css(".ui-datepicker-year"));
            return ParseMonth(month.TagName == "select" ? new SelectElement(month).SelectedOption.Text : month.Text,
                year.TagName == "select" ? new SelectElement(year).SelectedOption.Text : year.Text);
        }, Css(".ui-datepicker-prev"), Css(".ui-datepicker-next"),
        d => By.XPath($"//*[@id='ui-datepicker-div']//td[@data-month='{d.Month - 1}' and @data-year='{d.Year}']/a[normalize-space()='{d.Day}']"));
    }
    public void SelectDropdownYearMonth(DateOnly date)
    {
        new SelectElement(Wait.Clickable(Css("select.ui-datepicker-year"))).SelectByValue(date.Year.ToString(CultureInfo.InvariantCulture));
        new SelectElement(Wait.Clickable(Css("select.ui-datepicker-month"))).SelectByValue((date.Month - 1).ToString(CultureInfo.InvariantCulture));
    }
    public void ExpectDate(DateOnly date) => ExpectValue(Input, date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));
    public override void Dismiss() => Wait.Present(Input).SendKeys(Keys.Escape);
    public void ExpectClosed() => Wait.Gone(By.Id("ui-datepicker-div"));
}

public sealed class PracticePage(TestScope scope) : CalendarPage(scope, Url)
{
    public const string Url = "https://practice-automation.com/calendars/";
    public static readonly By Input = Css("input.jp-contact-form-date");
    public IMonthCalendar Open()
    {
        Wait.Click(Input);
        Wait.Visible(Css(".dp-cal"));
        return new DomMonthCalendar(Wait, () => ParseMonth(Text(".dp-cal-month"), Text(".dp-cal-year")),
            Css(".dp-prev"), Css(".dp-next"),
            d => Css($".dp-cal .dp-day:not(.dp-edge-day)[aria-label^='{d.ToString("ddd MMM dd yyyy", CalendarDates.English)}']"));
    }
    public void ExpectDate(DateOnly date) => ExpectValue(Input, CalendarDates.Iso(date));
    public void TypeDate(DateOnly date)
    {
        var input = Wait.Clickable(Input);
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(CalendarDates.Iso(date));
        input.SendKeys(Keys.Tab);
    }
    public void ExpectClosed() => Wait.Gone(Css(".dp-cal"));
}

public sealed class UkPage(TestScope scope) : CalendarPage(scope, Url)
{
    public const string Url = "https://www.automationtesting.co.uk/datepicker.html";
    public static readonly By Basic = By.Id("basicDate");
    public static readonly By Range = By.Id("rangeDate");
    public static readonly By Week = Css("input[placeholder='Select Date..']");
    public static readonly By Time = By.Id("timePicker");
    private const string Popup = ".flatpickr-calendar.open";
    public IMonthCalendar Open(By input)
    {
        Wait.Click(input);
        Wait.Visible(Css(Popup));
        return new DomMonthCalendar(Wait, () => ParseMonth(Text(Popup + " .cur-month"),
            Wait.Present(Css(Popup + " .cur-year")).GetDomProperty("value")!),
            Css(Popup + " .flatpickr-prev-month"), Css(Popup + " .flatpickr-next-month"),
            d => Css($"{Popup} .dayContainer .flatpickr-day:not(.prevMonthDay):not(.nextMonthDay)[aria-label='{d.ToString("MMMM d, yyyy", CalendarDates.English)}']"));
    }
    public void SetTime(int hour, int minute, string? meridiem = null)
    {
        SetHour(hour);
        Replace(Css(Popup + " .flatpickr-minute"), minute.ToString("00", CultureInfo.InvariantCulture));
        if (meridiem is not null && Text(Popup + " .flatpickr-am-pm") != meridiem) Wait.Click(Css(Popup + " .flatpickr-am-pm"));
        Dismiss();
    }
    private void Replace(By locator, string value)
    {
        var field = Wait.Clickable(locator);
        field.Clear();
        field.SendKeys(value);
        // Blur commits free-form minutes. Arrow keys would snap them to the five-minute step.
        field.SendKeys(Keys.Tab);
    }
    private void SetHour(int requested)
    {
        var locator = Css(Popup + " .flatpickr-hour");
        var field = Wait.Clickable(locator);
        var min = int.Parse(field.GetDomAttribute("data-min")!, CultureInfo.InvariantCulture);
        var max = int.Parse(field.GetDomAttribute("data-max")!, CultureInfo.InvariantCulture);
        if (requested < min || requested > max) throw new ArgumentOutOfRangeException(nameof(requested));
        for (var steps = 0; steps <= max - min + 1; steps++)
        {
            var current = int.Parse(Wait.Present(locator).GetDomProperty("value")!, CultureInfo.InvariantCulture);
            if (current == requested) return;
            // A real spinner step avoids this old widget's eager text normalization of zero.
            var expected = current == min ? max : current - 1;
            Wait.Present(locator).SendKeys(Keys.ArrowDown);
            Wait.Until(d => int.Parse(d.FindElement(locator).GetDomProperty("value")!, CultureInfo.InvariantCulture) == expected,
                $"Hour spinner transition to {expected}");
        }
        throw new InvalidOperationException("Hour spinner exceeded one full cycle.");
    }
    public void ExpectRange(DateOnly first, DateOnly last) => ExpectValue(Range, $"{CalendarDates.Iso(first)} to {CalendarDates.Iso(last)}");
    public void ExpectWeekNumbers() => Wait.Until(d => d.FindElements(Css(Popup + " .flatpickr-weeks .flatpickr-day")).Count >= 4, "Week numbers rendered");
    public void ResetWeek() { Dismiss(); Wait.Click(Css("button[data-clear][title='clear']")); ExpectValue(Week, ""); }
}
