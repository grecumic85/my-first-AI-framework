using Automation.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Automation.Database;

public sealed class SampleRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "New";
}
public sealed class ExampleDbContext(DbContextOptions<ExampleDbContext> options) : DbContext(options)
{
    public DbSet<SampleRecord> Records => Set<SampleRecord>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SampleRecord>().ToTable("automation_records");
        model.Entity<SampleRecord>().HasKey(x => x.Id);
        model.Entity<SampleRecord>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        model.Entity<SampleRecord>().Property(x => x.Status).HasMaxLength(30).IsRequired();
    }
}
public static class DatabaseFactory
{
    public static IDbContextFactory<ExampleDbContext> Create(DatabaseSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionString)) throw new ArgumentException("Configure Database.ConnectionString.");
        var builder = new DbContextOptionsBuilder<ExampleDbContext>();
        switch (settings.Provider)
        {
            case "PostgreSql": builder.UseNpgsql(settings.ConnectionString, p => p.EnableRetryOnFailure(3)); break;
            case "Sqlite": builder.UseSqlite(settings.ConnectionString); break;
            default: throw new ArgumentException("Database.Provider must be PostgreSql or Sqlite.");
        }
        return new ContextFactory<ExampleDbContext>(() => new ExampleDbContext(builder.Options));
    }
}
public sealed class ContextFactory<TContext>(Func<TContext> create) : IDbContextFactory<TContext> where TContext : DbContext
{
    public TContext CreateDbContext() => create();
    public Task<TContext> CreateDbContextAsync(CancellationToken token = default)
    { token.ThrowIfCancellationRequested(); return Task.FromResult(create()); }
}

// Allows EF CLI scaffolding without embedding a real connection string in source.
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<ExampleDbContext>
{
    public ExampleDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("AUTOMATION_Database__ConnectionString") ?? "Data Source=design-time.db";
        var provider = Environment.GetEnvironmentVariable("AUTOMATION_Database__Provider") ?? "Sqlite";
        return DatabaseFactory.Create(new DatabaseSettings { Provider = provider, ConnectionString = connection }).CreateDbContext();
    }
}
