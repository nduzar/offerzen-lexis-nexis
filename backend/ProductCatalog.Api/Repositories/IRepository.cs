namespace ProductCatalog.Api.Repositories;

public interface IRepository<TEntity, TKey>
    where TEntity : class
{
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken);

    Task<TEntity?> GetAsync(TKey id, CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);

    Task DeleteAsync(TKey id, CancellationToken cancellationToken);
}

public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    public abstract Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken);

    public abstract Task<TEntity?> GetAsync(TKey id, CancellationToken cancellationToken);

    public abstract Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    public abstract Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);

    public abstract Task DeleteAsync(TKey id, CancellationToken cancellationToken);
}
