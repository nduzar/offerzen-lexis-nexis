using ProductCatalog.Api.Domain;

namespace ProductCatalog.Api.Repositories;

public interface ICategoryRepository : IRepository<Category, Guid>
{
    Task<IReadOnlyList<Category>> ListRootsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> ListByParentAsync(Guid parentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryNode>> GetTreeAsync(CancellationToken cancellationToken);
}

public sealed record CategoryNode(Category Category, IReadOnlyList<CategoryNode> Children);
