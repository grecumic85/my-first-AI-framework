using System.Linq.Expressions;
using Automation.Core;
using Microsoft.EntityFrameworkCore;
namespace Automation.Database;

public interface IReadRepository<TEntity> where TEntity : class
{
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken token = default);
    Task<TEntity> WaitForAsync(Expression<Func<TEntity, bool>> predicate, TimeSpan timeout, CancellationToken token = default);
}
public interface IWriteRepository<TEntity> where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken token = default);
    Task<int> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken token = default);
}

public sealed class Repository<TContext, TEntity>(IDbContextFactory<TContext> factory, bool allowWrites = false)
    : IReadRepository<TEntity>, IWriteRepository<TEntity> where TContext : DbContext where TEntity : class
{
    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        return await db.Set<TEntity>().AsNoTracking().Where(predicate).ToListAsync(token);
    }
    public Task<TEntity> WaitForAsync(Expression<Func<TEntity, bool>> predicate, TimeSpan timeout, CancellationToken token = default)
        => Poll.UntilAsync(async ct =>
        {
            // A fresh context avoids stale tracking and is never shared by parallel tests.
            await using var db = await factory.CreateDbContextAsync(ct);
            return await db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(predicate, ct);
        }, row => row is not null, timeout, TimeSpan.FromMilliseconds(200), $"Database row {typeof(TEntity).Name}", token)!;
    public async Task AddAsync(TEntity entity, CancellationToken token = default)
    {
        RequireWrites();
        await using var db = await factory.CreateDbContextAsync(token);
        db.Set<TEntity>().Add(entity);
        await db.SaveChangesAsync(token);
    }
    public async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken token = default)
    {
        RequireWrites();
        await using var db = await factory.CreateDbContextAsync(token);
        return await db.Set<TEntity>().Where(predicate).ExecuteDeleteAsync(token);
    }
    private void RequireWrites()
    { if (!allowWrites) throw new InvalidOperationException("Database writes are disabled for this repository."); }
}

public sealed class DatabaseTransaction<TContext>(IDbContextFactory<TContext> factory) where TContext : DbContext
{
    // Rollback is default. Retried delegates must contain database work only, not HTTP calls or other side effects.
    public async Task ExecuteAsync(Func<TContext, CancellationToken, Task> work, bool commit = false, CancellationToken token = default)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(token);
        await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(token);
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            await work(db, token);
            await db.SaveChangesAsync(token);
            if (commit) await transaction.CommitAsync(token);
            else await transaction.RollbackAsync(token);
        });
    }
}
