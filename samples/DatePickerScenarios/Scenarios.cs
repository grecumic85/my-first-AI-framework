using Automation.Testing;

namespace DatePickerScenarios;

public sealed record CalendarScenario(string Id, string Site, string Description, Action<TestScope> Execute);

// Both NUnit and the console runner execute these exact same bodies.
public static class Scenarios
{
    public static IReadOnlyList<CalendarScenario> All { get; } = new CalendarScenario[]
    {
        new("LAT-01", "lastest", "Leap-month disabled weekends and exact selected-date message", scope =>
        {
            var p = new LastestPage(scope); var c = p.Calendar();
            var leap = CalendarDates.NextLeapDay(c.DisplayedMonth.Year);
            p.Navigator.GoTo(c, leap); p.ExpectWeekendsDisabled(leap);
            var selectable = CalendarDates.Weekend(leap) ? CalendarDates.WeekdayOnOrAfter(leap.AddDays(-3)) : leap;
            c.ClickDay(selectable); p.ExpectPicked(selectable);
        }),
        new("LAT-02", "lastest", "Cross-month range counts calendar nights including weekends", scope =>
        {
            var p = new LastestPage(scope); var c = p.Calendar(true);
            var next = c.DisplayedMonth.AddMonths(1);
            var start = next.AddDays(-1); while (CalendarDates.Weekend(start)) start = start.AddDays(-1);
            var end = CalendarDates.WeekdayOnOrAfter(next.AddDays(2));
            p.Navigator.Select(c, start); p.ExpectRangeStart(start);
            p.Navigator.Select(c, end); p.ExpectRange(start, end);
        }),
        new("LAT-03", "lastest", "Earlier second click restarts check-in before completing a new range", scope =>
        {
            var p = new LastestPage(scope); var c = p.Calendar(true);
            var late = CalendarDates.WeekdayOnOrAfter(c.DisplayedMonth.AddDays(15));
            var early = CalendarDates.WeekdayOnOrAfter(c.DisplayedMonth.AddDays(3));
            p.Navigator.Select(c, late); p.ExpectRangeStart(late);
            p.Navigator.Select(c, early); p.ExpectRangeStart(early);
            p.Navigator.Select(c, late); p.ExpectRange(early, late);
        }),
        new("LAT-04", "lastest", "Reset completed range preserves the independent single calendar", scope =>
        {
            var p = new LastestPage(scope); var single = p.Calendar();
            var day = CalendarDates.WeekdayOnOrAfter(single.DisplayedMonth.AddDays(6));
            p.Navigator.Select(single, day); p.ExpectPicked(day);
            var range = p.Calendar(true); p.Navigator.Select(range, day);
            var end = CalendarDates.WeekdayOnOrAfter(day.AddDays(3)); p.Navigator.Select(range, end); p.ExpectRange(day, end);
            p.ResetRange(); p.ExpectPicked(day);
            p.Navigator.Select(range, end); p.ExpectRangeStart(end);
        }),
        new("LAT-05", "lastest", "Range crosses December/January and validates endpoint availability", scope =>
        {
            var p = new LastestPage(scope); var c = p.Calendar(true);
            var start = new DateOnly(c.DisplayedMonth.Year, 12, 31); while (CalendarDates.Weekend(start)) start = start.AddDays(-1);
            var end = CalendarDates.WeekdayOnOrAfter(new DateOnly(start.Year + 1, 1, 2));
            p.Navigator.Select(c, start); p.ExpectRangeStart(start);
            p.Navigator.Select(c, end); p.ExpectRange(start, end); p.ExpectWeekendsDisabled(end, true);
        }),
        new("GSQ-01", "globalsqa", "Iframe simple picker selects leap day and closes", scope =>
        {
            var p = new GlobalSqaPage(scope); var c = p.Open(); var date = CalendarDates.NextLeapDay(c.DisplayedMonth.Year);
            p.Navigator.Select(c, date); p.ExpectDate(date); p.ExpectClosed();
        }),
        new("GSQ-02", "globalsqa", "Backward navigation across year boundary replaces existing selection", scope =>
        {
            var p = new GlobalSqaPage(scope); var c = p.Open(); var january = new DateOnly(c.DisplayedMonth.Year, 1, 1);
            p.Navigator.Select(c, january); p.ExpectDate(january);
            c = p.Open(); p.Navigator.Select(c, january.AddDays(-1)); p.ExpectDate(january.AddDays(-1)); p.ExpectClosed();
        }),
        new("GSQ-03", "globalsqa", "Month/year dropdowns select a distant leap day inside the correct iframe", scope =>
        {
            var p = new GlobalSqaPage(scope); p.EnterDemo("DropDown DatePicker", "dropdown-month-year.html");
            var c = p.Open(); var date = CalendarDates.NextLeapDay(c.DisplayedMonth.Year + 1);
            p.SelectDropdownYearMonth(date); c.ClickDay(date); p.ExpectDate(date); p.ExpectClosed();
        }),
        new("GSQ-04", "globalsqa", "Icon trigger chooses month end and reopens at selected month", scope =>
        {
            var p = new GlobalSqaPage(scope); p.EnterDemo("Icon Trigger", "icon-trigger.html");
            var c = p.Open(true); var date = CalendarDates.NextLeapDay(c.DisplayedMonth.Year);
            p.Navigator.Select(c, date); p.ExpectDate(date); p.ExpectClosed();
            c = p.Open(true); Equal(c.DisplayedMonth, CalendarDates.Month(date)); p.Dismiss(); p.ExpectDate(date);
        }),
        new("GSQ-05", "globalsqa", "Escape after browsing another month preserves previously committed value", scope =>
        {
            var p = new GlobalSqaPage(scope); var c = p.Open(); var date = c.DisplayedMonth.AddDays(14);
            c.ClickDay(date); p.ExpectDate(date); c = p.Open(); c.MoveMonth(1);
            p.Dismiss(); p.ExpectClosed(); p.ExpectDate(date);
        }),
        new("PRA-01", "practice", "Leap day selection produces ISO date and closes the popup", scope =>
        {
            var p = new PracticePage(scope); var c = p.Open(); var date = CalendarDates.NextLeapDay(c.DisplayedMonth.Year);
            p.Navigator.Select(c, date); p.ExpectDate(date); p.ExpectClosed();
        }),
        new("PRA-02", "practice", "December/January navigation selects exact dates despite adjacent-month cells", scope =>
        {
            var p = new PracticePage(scope); var c = p.Open(); var date = new DateOnly(c.DisplayedMonth.Year, 12, 31);
            p.Navigator.Select(c, date); p.ExpectDate(date);
            c = p.Open(); p.Navigator.Select(c, date.AddDays(1)); p.ExpectDate(date.AddDays(1));
        }),
        new("PRA-03", "practice", "Typed ISO leap date synchronizes calendar then UI selection replaces it", scope =>
        {
            var p = new PracticePage(scope); var c = p.Open(); var date = CalendarDates.NextLeapDay(c.DisplayedMonth.Year);
            p.Dismiss(); p.TypeDate(date); p.ExpectDate(date); c = p.Open(); Equal(c.DisplayedMonth, CalendarDates.Month(date));
            p.Navigator.Select(c, date.AddDays(1)); p.ExpectDate(date.AddDays(1));
        }),
        new("PRA-04", "practice", "Escape cancels month browsing and preserves committed date", scope =>
        {
            var p = new PracticePage(scope); var c = p.Open(); var date = c.DisplayedMonth.AddDays(9);
            c.ClickDay(date); p.ExpectDate(date); c = p.Open(); c.MoveMonth(-1);
            p.Dismiss(); p.ExpectClosed(); p.ExpectDate(date);
        }),
        new("UK-01", "uk", "Leap day and PM time are formatted into the combined field", scope =>
        {
            var p = new UkPage(scope); var c = p.Open(UkPage.Basic); var date = CalendarDates.NextLeapDay(c.DisplayedMonth.Year);
            p.Navigator.Select(c, date); p.SetTime(11, 45, "PM");
            p.ExpectValue(UkPage.Basic, date.ToString("MMMM, dd yyyy", CalendarDates.English) + " 23:45");
        }),
        new("UK-02", "uk", "Range selection crosses year boundary with exact serialized endpoints", scope =>
        {
            var p = new UkPage(scope); var c = p.Open(UkPage.Range); var start = new DateOnly(c.DisplayedMonth.Year, 12, 31); var end = start.AddDays(2);
            p.Navigator.Select(c, start); p.Navigator.Select(c, end); p.ExpectRange(start, end);
        }),
        new("UK-03", "uk", "Reversed range selection sorts endpoints across leap day", scope =>
        {
            var p = new UkPage(scope); var c = p.Open(UkPage.Range); var leap = CalendarDates.NextLeapDay(c.DisplayedMonth.Year);
            p.Navigator.Select(c, leap.AddDays(1)); p.Navigator.Select(c, leap.AddDays(-1)); p.ExpectRange(leap.AddDays(-1), leap.AddDays(1));
        }),
        new("UK-04", "uk", "Week-number picker reset clears its value while preserving another range", scope =>
        {
            var p = new UkPage(scope); var c = p.Open(UkPage.Range); var start = c.DisplayedMonth.AddDays(4); var end = start.AddDays(3);
            c.ClickDay(start); c.ClickDay(end); p.ExpectRange(start, end);
            c = p.Open(UkPage.Week); p.ExpectWeekNumbers(); c.ClickDay(start); p.ExpectValue(UkPage.Week, CalendarDates.Iso(start));
            p.ResetWeek(); p.ExpectRange(start, end);
            c = p.Open(UkPage.Week); c.ClickDay(end); p.ExpectValue(UkPage.Week, CalendarDates.Iso(end));
        }),
        new("UK-05", "uk", "Time-only picker accepts midnight and end-of-day minute boundary", scope =>
        {
            var p = new UkPage(scope); p.Open(UkPage.Time); p.SetTime(0, 0); p.ExpectValue(UkPage.Time, "00:00");
            p.Open(UkPage.Time); p.SetTime(23, 59); p.ExpectValue(UkPage.Time, "23:59");
        })
    };
    private static void Equal<T>(T actual, T expected)
    { if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new InvalidOperationException($"Expected {expected}; actual {actual}."); }
}
