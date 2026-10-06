using System.Net;
using System.Text;
using Automation.Documents;
using Automation.Emulation;
using Automation.Generated.Sample;
using Automation.Integrations;
using Automation.Selenium;
using NUnit.Framework;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Core;
namespace Automation.TestExamples;

[TestFixture]
public sealed class IntegrationAdapterTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(respond(request));
    }
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    [Test]
    public async Task Generated_client_invocation_can_define_an_emulator_mapping()
    {
        var template = await RequestCapture.CaptureAsync(async http => await new SampleClient(http).GetHealthAsync(), "{\"status\":\"ok\"}");
        Assert.That(template.Method, Is.EqualTo("GET"));
        Assert.That(template.Path, Is.EqualTo("/health"));
    }
    [Test]
    public async Task Mailhog_adapter_parses_search_response()
    {
        using var http = new HttpClient(new Handler(_ => Json("""
            {"items":[{"ID":"sample-id","Content":{"Headers":{"Subject":["Welcome test"]},"Body":"Email body"}}]}
            """))) { BaseAddress = new Uri("http://localhost/") };
        var email = await new MailhogClient(http).WaitAsync("unique@example.invalid", "Welcome", TimeSpan.FromSeconds(1));
        Assert.That(email.Body, Is.EqualTo("Email body"));
    }
    [Test]
    public async Task Zephyr_resolves_execution_in_requested_phase()
    {
        var posted = false;
        using var http = new HttpClient(new Handler(request =>
        {
            if (request.Method == HttpMethod.Get) return Json("""
                [{"results":[{"cyclePhaseId":"wrong","id":"10"},{"cyclePhaseId":"wanted","id":"20"}]}]
                """);
            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.That(body, Does.Contain("\"releaseTestScheduleId\":\"20\""));
            posted = true;
            return Json("{}");
        })) { BaseAddress = new Uri("http://localhost/") };
        await new ZephyrEnterprisePublisher(http, new ZephyrSettings("release", "wanted", "tester"))
            .PublishAsync(new TestResult("test", "Passed", 1, ["case"]));
        Assert.That(posted, Is.True);
    }
    [Test]
    public async Task Grid_capacity_filters_browser_and_used_slots()
    {
        using var http = new HttpClient(new Handler(_ => Json("""
            {"value":{"ready":true,"nodes":[{"availability":"UP","slots":[
            {"session":null,"stereotype":{"browserName":"chrome"}},
            {"session":{"id":"used"},"stereotype":{"browserName":"firefox"}}]}]}}
            """))) { BaseAddress = new Uri("http://localhost/") };
        var grid = new GridCapacity(http);
        Assert.That(await grid.HasFreeSlotAsync("chrome"), Is.True);
        Assert.That(await grid.HasFreeSlotAsync("firefox"), Is.False);
    }
    [Test]
    public void Pdf_text_extraction_reads_a_real_generated_pdf()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(300, 300);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Sample invoice", 12, new PdfPoint(20, 200), font);
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", "sample.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, builder.Build());
        Assert.That(new PdfTextExtractor().Read(path), Does.Contain("Sample invoice"));
    }
    [Test]
    public async Task Download_wait_does_not_accept_partial_files()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path, "sample.csv"), "complete");
        await File.WriteAllTextAsync(Path.Combine(path, "sample.csv.crdownload"), "pending");
        Assert.ThrowsAsync<TimeoutException>(async () => await Downloads.WaitForFileAsync(path, "sample.csv", TimeSpan.FromMilliseconds(100)));
        File.Delete(Path.Combine(path, "sample.csv.crdownload"));
        Assert.That(await Downloads.WaitForFileAsync(path, "sample.csv", TimeSpan.FromSeconds(2)), Does.EndWith("sample.csv"));
    }
}
