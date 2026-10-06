using System.Net;
using System.Text;
using Automation.Api;
using Automation.Core;
using NUnit.Framework;
namespace Automation.TestExamples;

[TestFixture]
public sealed class HttpTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return Task.FromResult(respond(request)); }
    }
    [TestCase("GET", 3)]
    [TestCase("POST", 1)]
    public async Task Transient_retry_does_not_replay_writes(string method, int expectedCalls)
    {
        var calls = 0;
        var handler = new Handler(_ => new HttpResponseMessage(++calls < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
        using var http = ApiClientFactory.Create(new Uri("http://localhost/"), new NoTokenProvider(), new NullEventSink(), handler);
        using var request = new HttpRequestMessage(new HttpMethod(method), "test");
        using var response = await http.SendAsync(request);
        Assert.That(handler.Calls, Is.EqualTo(expectedCalls));
    }
    [Test]
    public async Task Auth_failures_are_not_retried()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var http = ApiClientFactory.Create(new Uri("http://localhost/"), new NoTokenProvider(), new NullEventSink(), handler);
        using var response = await http.GetAsync("test");
        Assert.That(handler.Calls, Is.EqualTo(1));
    }
    [Test]
    public async Task Token_cache_serializes_parallel_refreshes()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("{\"access_token\":\"sample-token\",\"expires_in\":3600}", Encoding.UTF8, "application/json") });
        using var http = new HttpClient(handler);
        using var provider = new ClientCredentialsTokenProvider(http, new AuthSettings { TokenEndpoint = "http://localhost/token" });
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => provider.GetAsync()));
        Assert.That(handler.Calls, Is.EqualTo(1));
        Assert.That(results, Is.All.EqualTo("sample-token"));
    }
    [Test]
    public async Task Api_audit_omits_secrets_bodies_and_query_strings()
    {
        var root = new ArtifactDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts"), "audit");
        var handler = new Handler(request =>
        {
            Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo("sensitive-token"));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("private-body") };
        });
        using var http = ApiClientFactory.Create(new Uri("http://localhost/"), new StaticTokenProvider("sensitive-token"), new JsonEventSink(root.Path), handler);
        using var response = await http.GetAsync("test?secret=private-query");
        var log = await File.ReadAllTextAsync(root.File("events.jsonl"));
        Assert.That(log, Does.Not.Contain("sensitive-token").And.Not.Contain("private-body").And.Not.Contain("private-query"));
    }
    [Test]
    public void Json_client_rejects_other_origins()
    {
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost/") };
        Assert.ThrowsAsync<ArgumentException>(async () => await new JsonApiClient(http).GetAsync<object>("https://other.example/"));
    }
}
