using ProductCatalog.Api.Domain;

namespace ProductCatalog.Api.Repositories;

public sealed class InMemoryCategoryRepository : Repository<Category, Guid>, ICategoryRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Category> _categories = new();

    public InMemoryCategoryRepository()
    {
        var root = new Category
        {
            Id = Guid.NewGuid(),
            Name = "All",
            Description = "Root category",
            ParentCategoryId = null
        };

        var electronics = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics",
            Description = null,
            ParentCategoryId = root.Id
        };

        var laptops = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Laptops",
            Description = null,
            ParentCategoryId = electronics.Id
        };

        _categories[root.Id] = root;
        _categories[electronics.Id] = electronics;
        _categories[laptops.Id] = laptops;
    }

    public override Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult((IReadOnlyList<Category>)_categories.Values.OrderBy(c => c.Name).ToList());
        }
    }

    public override Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _categories.TryGetValue(id, out var category);
            return Task.FromResult(category);
        }
    }

    public override Task AddAsync(Category entity, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _categories[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    public override Task UpdateAsync(Category entity, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _categories[entity.Id] = entity;
            return Task.CompletedTask;
        }
    }

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _categories.Remove(id);

            var children = _categories.Values.Where(c => c.ParentCategoryId == id).Select(c => c.Id).ToList();
            foreach (var childId in children)
            {
                _categories.Remove(childId);
            }

            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<Category>> ListRootsAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult((IReadOnlyList<Category>)_categories.Values
                .Where(c => c.ParentCategoryId is null)
                .OrderBy(c => c.Name)
                .ToList());
        }
    }

    public Task<IReadOnlyList<Category>> ListByParentAsync(Guid parentId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult((IReadOnlyList<Category>)_categories.Values
                .Where(c => c.ParentCategoryId == parentId)
                .OrderBy(c => c.Name)
                .ToList());
        }
    }

    public Task<IReadOnlyList<CategoryNode>> GetTreeAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var roots = _categories.Values
                .Where(c => c.ParentCategoryId is null)
                .OrderBy(c => c.Name)
                .ToList();

            var byParent = _categories.Values
                .Where(c => c.ParentCategoryId is not null)
                .GroupBy(c => c.ParentCategoryId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());

            IReadOnlyList<CategoryNode> Build(Guid parentId)
            {
                if (!byParent.TryGetValue(parentId, out var children))
                {
                    return Array.Empty<CategoryNode>();
                }

                return children
                    .Select(c => new CategoryNode(c, Build(c.Id)))
                    .ToList();
            }

            var tree = roots
                .Select(r => new CategoryNode(r, Build(r.Id)))
                .ToList();

            return Task.FromResult((IReadOnlyList<CategoryNode>)tree);
        }
    }
}
