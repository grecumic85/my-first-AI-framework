using Automation.Testing;
using DatePickerScenarios;
using NUnit.Framework;

namespace Automation.DatePickers;

[Category("External")]
[Category("DatePicker")]
[NonParallelizable]
public sealed class DatePickerTests : AutomationTest
{
    public static IEnumerable<TestCaseData> Cases => Scenarios.All.Select(s =>
        new TestCaseData(s.Id).SetName($"{s.Id}: {s.Description}").SetCategory(s.Site));

    [TestCaseSource(nameof(Cases))]
    public void Calendar_behavior(string id)
    {
        if (Environment.GetEnvironmentVariable("AUTOMATION_LIVE_DATEPICKERS") != "1")
            Assert.Ignore("Public-site tests are opt-in. Set AUTOMATION_LIVE_DATEPICKERS=1.");
        Scenarios.All.Single(s => s.Id == id).Execute(Scope);
    }
}
