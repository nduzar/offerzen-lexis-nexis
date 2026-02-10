using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Domain;
using ProductCatalog.Api.Search;

namespace ProductCatalog.Api.Repositories;

public sealed class ProductRepository(AppDbContext db) : Repository<Product, Guid>, IProductRepository
{
    public override async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Products.AsNoTracking().ToListAsync(cancellationToken);
    }

    public override async Task<Product?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public override async Task AddAsync(Product entity, CancellationToken cancellationToken)
    {
        db.Products.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public override async Task UpdateAsync(Product entity, CancellationToken cancellationToken)
    {
        db.Products.Update(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public override async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        db.Products.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListPagedAsync(
        int page,
        int pageSize,
        Guid? categoryId,
        string? name,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();
        query = query.ApplyCatalogFilters(categoryId, name);

        return await query
            .OrderBy(p => p)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Guid? categoryId, string? name, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();
        query = query.ApplyCatalogFilters(categoryId, name);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        return await db.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }
}
