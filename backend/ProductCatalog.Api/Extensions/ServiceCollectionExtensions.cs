using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Repositories;
using ProductCatalog.Api.Search;

namespace ProductCatalog.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductCatalog(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("ProductCatalog"));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();
        services.AddSingleton<ProductSearchEngine>();

        return services;
    }
}
