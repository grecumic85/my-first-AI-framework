using Automation.Core;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
namespace Automation.Testing;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class AutomationTest
{
    protected TestScope Scope { get; private set; } = null!;
    [SetUp]
    public void SetUpScope() => Scope = new TestScope(AutomationSettings.Load(TestContext.CurrentContext.TestDirectory), TestContext.CurrentContext.Test.FullName);
    [TearDown]
    public async Task TearDownScope()
    {
        if (Scope is null) return;
        var status = TestContext.CurrentContext.Result.Outcome.Status;
        await Scope.CompleteAsync(status == TestStatus.Passed ? "Passed" : status == TestStatus.Skipped ? "Skipped" : "Failed");
        foreach (var file in Directory.EnumerateFiles(Scope.Artifacts.Path)) TestContext.AddTestAttachment(file);
    }
}
