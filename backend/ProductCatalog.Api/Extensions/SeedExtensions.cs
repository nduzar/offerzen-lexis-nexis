using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Domain;
using ProductCatalog.Api.Repositories;

namespace ProductCatalog.Api.Extensions;

public static class SeedExtensions
{
    public static async Task SeedAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categories = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        await db.Database.EnsureCreatedAsync();

        if (await db.Products.AnyAsync())
        {
            return;
        }

        var categoryList = await categories.ListAsync(CancellationToken.None);
        var defaultCategoryId = categoryList.FirstOrDefault(c => c.ParentCategoryId is not null)?.Id
            ?? categoryList.FirstOrDefault()?.Id
            ?? Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        db.Products.AddRange(
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Laptop Pro 14",
                Description = "High performance laptop",
                Sku = "LTP-014-PRO",
                Price = 1999.99m,
                Quantity = 12,
                CategoryId = defaultCategoryId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse",
                Sku = "MSE-WRL-001",
                Price = 29.99m,
                Quantity = 80,
                CategoryId = defaultCategoryId,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "USB-C Hub",
                Description = "6-in-1 adapter",
                Sku = "HUB-USBC-006",
                Price = 49.50m,
                Quantity = 35,
                CategoryId = defaultCategoryId,
                CreatedAt = now,
                UpdatedAt = now
            });

        await db.SaveChangesAsync();
    }
}
