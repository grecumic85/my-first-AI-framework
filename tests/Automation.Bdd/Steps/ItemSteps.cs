using Automation.Core;
using Automation.Generated.Sample;
using Automation.TestExamples;
using Automation.Testing;
using NUnit.Framework;
using Reqnroll;
namespace Automation.Bdd;

[Binding]
public sealed class ItemSteps(ScenarioContext scenario)
{
    private SampleServer? server;
    private TestScope? scope;
    private SampleClient? client;
    private Item? created;
    [BeforeScenario]
    public async Task Setup()
    {
        server = await SampleServer.StartAsync();
        var settings = new AutomationSettings { BaseUrl = server.BaseUrl.ToString(), ApiBaseUrl = server.BaseUrl.ToString() };
        scope = new TestScope(settings, scenario.ScenarioInfo.Title);
        client = new SampleClient(scope.ApiHttp);
        // Each binding object belongs to its scenario; @NoBrowser preserves API-only scenarios.
        if (!scenario.ScenarioInfo.CombinedTags.Contains("NoBrowser")) scope.Browsers.Start();
    }
    [Given("an isolated application")]
    public async Task GivenApplication() => Assert.That((await client!.GetHealthAsync()).Status, Is.EqualTo("ok"));
    [When("I create an item named {string}")]
    public async Task CreateItem(string name)
    {
        created = await client!.CreateItemAsync(new CreateItem { Name = name });
        var id = created.Id;
        scope!.Cleanup.Register($"item {id}", ct => client.DeleteItemAsync(id, ct));
    }
    [Then("the item can be read from the API")]
    public async Task AssertItem() => Assert.That((await client!.GetItemAsync(created!.Id)).Name, Is.EqualTo(created.Name));
    [AfterScenario]
    public async Task Teardown()
    {
        try
        {
            if (scope is not null) await scope.CompleteAsync(scenario.TestError is null ? "Passed" : "Failed",
                scenario.ScenarioInfo.CombinedTags.Where(t => t.StartsWith("CaseId_")).Select(t => t[7..]).ToArray());
        }
        finally { if (server is not null) await server.DisposeAsync(); }
    }
}
