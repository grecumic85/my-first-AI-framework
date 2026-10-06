using Automation.Core;
using Automation.Database;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
namespace Automation.TestExamples;

[TestFixture, FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class DatabaseTests
{
    private IDbContextFactory<ExampleDbContext> factory = null!;
    [SetUp]
    public async Task Setup()
    {
        var artifacts = new ArtifactDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts"), "db");
        factory = DatabaseFactory.Create(new DatabaseSettings { Provider = "Sqlite", ConnectionString = $"Data Source={artifacts.File("test.db")}" });
        await using var db = factory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }
    [Test]
    public async Task Repository_reads_are_detached_and_writes_are_opt_in()
    {
        var readOnly = new Repository<ExampleDbContext, SampleRecord>(factory);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await readOnly.AddAsync(new SampleRecord()));
        var writable = new Repository<ExampleDbContext, SampleRecord>(factory, true);
        var id = Guid.NewGuid();
        await writable.AddAsync(new SampleRecord { Id = id, Name = "test" });
        var rows = await readOnly.FindAsync(r => r.Id == id);
        rows[0].Name = "changed only in memory";
        Assert.That((await readOnly.FindAsync(r => r.Id == id))[0].Name, Is.EqualTo("test"));
    }
    [Test]
    public async Task Database_polling_observes_changes_from_a_different_context()
    {
        var repository = new Repository<ExampleDbContext, SampleRecord>(factory, true);
        var id = Guid.NewGuid();
        var wait = repository.WaitForAsync(r => r.Id == id, TimeSpan.FromSeconds(3));
        await repository.AddAsync(new SampleRecord { Id = id, Name = "eventual" });
        Assert.That((await wait).Name, Is.EqualTo("eventual"));
    }
    [Test]
    public async Task Transactions_roll_back_by_default_and_commit_when_requested()
    {
        var transaction = new DatabaseTransaction<ExampleDbContext>(factory);
        var repository = new Repository<ExampleDbContext, SampleRecord>(factory);
        await transaction.ExecuteAsync((db, _) => { db.Records.Add(new SampleRecord { Id = Guid.NewGuid(), Name = "rollback" }); return Task.CompletedTask; });
        Assert.That(await repository.FindAsync(_ => true), Is.Empty);
        await transaction.ExecuteAsync((db, _) => { db.Records.Add(new SampleRecord { Id = Guid.NewGuid(), Name = "commit" }); return Task.CompletedTask; }, commit: true);
        Assert.That((await repository.FindAsync(_ => true)).Count, Is.EqualTo(1));
    }
}
