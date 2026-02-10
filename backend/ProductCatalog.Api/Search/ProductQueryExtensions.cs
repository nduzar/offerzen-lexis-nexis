using ProductCatalog.Api.Domain;

namespace ProductCatalog.Api.Search;

public static class ProductQueryExtensions
{
    public static IQueryable<Product> ApplyCatalogFilters(this IQueryable<Product> query, Guid? categoryId, string? name)
    {
        if (categoryId is Guid catId)
        {
            query = query.Where(p => p.CategoryId == catId);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.WhereNameContains(name);
        }

        return query;
    }

    public static IQueryable<Product> WhereNameContains(this IQueryable<Product> query, string name)
    {
        var term = name.Trim();
        if (term.Length == 0)
        {
            return query;
        }

        return query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<Product> WhereHasInventory(this IEnumerable<Product> products)
    {
        return products.Where(p => p.Quantity > 0);
    }
}
