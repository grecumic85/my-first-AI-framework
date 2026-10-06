using System.Diagnostics;
using System.Globalization;
using Automation.Selenium;
using OpenQA.Selenium;

namespace DatePickerScenarios;

// Narrow contract: no assumption about ranges, disabled dates, text entry, or popup lifetime.
public interface IMonthCalendar
{
    DateOnly DisplayedMonth { get; }
    void MoveMonth(int direction);
    void ClickDay(DateOnly date);
}

public static class CalendarDates
{
    public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    public static DateOnly Month(DateOnly value) => new(value.Year, value.Month, 1);
    public static int Distance(DateOnly from, DateOnly to) => (to.Year - from.Year) * 12 + to.Month - from.Month;
    public static string Iso(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static DateOnly NextLeapDay(int year)
    {
        while (!DateTime.IsLeapYear(year)) year++;
        return new(year, 2, 29);
    }
    public static bool Weekend(DateOnly day) => day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static DateOnly WeekdayOnOrAfter(DateOnly day) { while (Weekend(day)) day = day.AddDays(1); return day; }
}

public sealed class MonthNavigator(int maximumMonths = 120)
{
    public void GoTo(IMonthCalendar calendar, DateOnly date)
    {
        if (maximumMonths < 1) throw new ArgumentOutOfRangeException(nameof(maximumMonths));
        var target = CalendarDates.Month(date);
        var current = calendar.DisplayedMonth;
        var distance = CalendarDates.Distance(current, target);
        if (Math.Abs(distance) > maximumMonths)
            throw new ArgumentOutOfRangeException(nameof(date), $"Target exceeds the {maximumMonths}-month navigation budget.");
        var watch = Stopwatch.StartNew();
        for (var remaining = Math.Abs(distance); remaining > 0; remaining--)
        {
            if (watch.Elapsed > TimeSpan.FromMinutes(2)) throw new TimeoutException("Calendar navigation exceeded two minutes.");
            var direction = Math.Sign(distance);
            calendar.MoveMonth(direction);
            var next = calendar.DisplayedMonth;
            if (next != current.AddMonths(direction))
                throw new InvalidOperationException($"Calendar did not move exactly one month: {current} -> {next}.");
            current = next;
        }
        if (current != target) throw new InvalidOperationException("Calendar navigation ended in the wrong month.");
    }
    public void Select(IMonthCalendar calendar, DateOnly date) { GoTo(calendar, date); calendar.ClickDay(date); }
}

// Selector/format differences are injected; the wait-and-transition algorithm has one implementation.
public sealed class DomMonthCalendar(BrowserWait wait, Func<DateOnly> readMonth,
    By previous, By next, Func<DateOnly, By> day) : IMonthCalendar
{
    public DateOnly DisplayedMonth => CalendarDates.Month(readMonth());
    public void MoveMonth(int direction)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        var expected = DisplayedMonth.AddMonths(direction);
        wait.Click(direction < 0 ? previous : next);
        wait.Until(_ => DisplayedMonth == expected, $"Calendar month {expected:yyyy-MM}");
    }
    public void ClickDay(DateOnly date)
    {
        if (DisplayedMonth != CalendarDates.Month(date)) throw new InvalidOperationException("Navigate to the target month before selecting a day.");
        wait.Click(day(date));
    }
}
