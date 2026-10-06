using DatePickerScenarios;

internal static class NavigationChecks
{
    public static int Run()
    {
        var checks = new (string Name, Action Run)[]
        {
            ("Forward year boundary", () => { var c = new FakeCalendar(new(2027, 12, 1)); new MonthNavigator().Select(c, new(2028, 1, 31)); Equal(c.DisplayedMonth, new DateOnly(2028, 1, 1)); Equal(c.Selected, new DateOnly(2028, 1, 31)); Equal(c.Moves, 1); }),
            ("Backward leap-year boundary", () => { var c = new FakeCalendar(new(2028, 3, 1)); new MonthNavigator().Select(c, new(2028, 2, 29)); Equal(c.Moves, 1); Equal(c.Selected, new DateOnly(2028, 2, 29)); }),
            ("Already at target makes no navigation clicks", () => { var c = new FakeCalendar(new(2028, 2, 1)); new MonthNavigator().Select(c, new(2028, 2, 29)); Equal(c.Moves, 0); }),
            ("Budget rejected before any UI change", () => { var c = new FakeCalendar(new(2028, 1, 1)); Throws<ArgumentOutOfRangeException>(() => new MonthNavigator(2).GoTo(c, new(2028, 4, 1))); Equal(c.Moves, 0); }),
            ("Stuck calendar fails instead of infinite looping", () => { var c = new FakeCalendar(new(2028, 1, 1)) { Stuck = true }; Throws<InvalidOperationException>(() => new MonthNavigator().GoTo(c, new(2028, 2, 1))); Equal(c.Moves, 1); }),
            ("Skipped month is detected", () => { var c = new FakeCalendar(new(2028, 1, 1)) { Step = 2 }; Throws<InvalidOperationException>(() => new MonthNavigator().GoTo(c, new(2028, 3, 1))); }),
            ("Century leap rules and calendar-night arithmetic", () => { Equal(CalendarDates.NextLeapDay(2097), new DateOnly(2104, 2, 29)); Equal(new DateOnly(2028, 3, 1).DayNumber - new DateOnly(2028, 2, 28).DayNumber, 2); }),
            ("Weekend normalization crosses month/year", () => Equal(CalendarDates.WeekdayOnOrAfter(new DateOnly(2022, 12, 31)), new DateOnly(2023, 1, 2)))
        };
        var failed = 0;
        foreach (var check in checks) try { check.Run(); Console.WriteLine("PASS " + check.Name); } catch (Exception ex) { failed++; Console.WriteLine("FAIL " + check.Name + ": " + ex.Message); }
        Console.WriteLine($"{checks.Length - failed}/{checks.Length} local navigation checks passed."); return failed == 0 ? 0 : 1;
    }
    private static void Equal<T>(T actual, T expected) { if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new Exception($"Expected {expected}; got {actual}"); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    private sealed class FakeCalendar(DateOnly initial) : IMonthCalendar
    {
        public DateOnly DisplayedMonth { get; private set; } = initial;
        public DateOnly? Selected { get; private set; }
        public int Moves { get; private set; }
        public bool Stuck { get; init; }
        public int Step { get; init; } = 1;
        public void MoveMonth(int direction) { Moves++; if (!Stuck) DisplayedMonth = DisplayedMonth.AddMonths(direction * Step); }
        public void ClickDay(DateOnly date) => Selected = date;
    }
}
