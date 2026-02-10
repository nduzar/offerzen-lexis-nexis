using ProductCatalog.Api.Domain;

namespace ProductCatalog.Api.Repositories;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<IReadOnlyList<Product>> ListPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? name,
        CancellationToken cancellationToken);

    Task<int> CountAsync(Guid? categoryId, string? name, CancellationToken cancellationToken);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken);
}
