using System.Collections.Concurrent;
using System.Text.Json;

var app = SampleApp.AppHost.Build(args);
await app.RunAsync();

namespace SampleApp
{
    public static class AppHost
    {
        public static WebApplication Build(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            var items = new ConcurrentDictionary<Guid, Item>();
            var settings = new ConcurrentDictionary<string, JsonElement>();
            var benchmarks = new ConcurrentDictionary<Guid, Benchmark>();
            app.MapGet("/", async () =>
            {
                using var stream = typeof(AppHost).Assembly.GetManifestResourceStream("SampleApp.wwwroot.index.html")!;
                using var reader = new StreamReader(stream);
                return Results.Content(await reader.ReadToEndAsync(), "text/html");
            });
            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
            app.MapGet("/api/items", () => Results.Ok(items.Values.OrderBy(i => i.Name)));
            app.MapPost("/api/items", (CreateItem request) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest();
                var item = new Item(Guid.NewGuid(), request.Name, "New");
                items[item.Id] = item;
                return Results.Created($"/api/items/{item.Id}", item);
            });
            app.MapGet("/api/items/{id:guid}", (Guid id) => items.TryGetValue(id, out var item) ? Results.Ok(item) : Results.NotFound());
            app.MapDelete("/api/items/{id:guid}", (Guid id) => { items.TryRemove(id, out _); return Results.NoContent(); });
            app.MapPost("/admin/transaction-settings/{name}", (string name, JsonElement value) => { settings[name] = value.Clone(); return Results.NoContent(); });
            app.MapGet("/admin/transaction-settings/{name}", (string name) => settings.TryGetValue(name, out var value) ? Results.Ok(value) : Results.NotFound());
            app.MapDelete("/admin/transaction-settings/{name}", (string name) => { settings.TryRemove(name, out _); return Results.NoContent(); });
            app.MapPost("/admin/benchmarks", (Benchmark value) => { benchmarks[value.Id] = value; return Results.Created($"/admin/benchmarks/{value.Id}", value); });
            app.MapGet("/admin/benchmarks", (HttpRequest request) => Results.Ok(benchmarks.Values.Where(b => request.Query.All(q => b.Match.TryGetValue(q.Key, out var v) && v == q.Value.ToString()))));
            app.MapDelete("/admin/benchmarks/{id:guid}", (Guid id) => { benchmarks.TryRemove(id, out _); return Results.NoContent(); });
            app.MapGet("/download", () => Results.File(System.Text.Encoding.UTF8.GetBytes("id,name\n1,Sample\n"), "text/csv", "sample.csv"));
            return app;
        }
    }
    public sealed record Item(Guid Id, string Name, string Status);
    public sealed record CreateItem(string Name);
    public sealed record Benchmark(Guid Id, Dictionary<string, string> Match, JsonElement Response);
}
