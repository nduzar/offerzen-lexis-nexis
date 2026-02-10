using System.Reflection;
using ProductCatalog.Api.Search;

namespace ProductCatalog.Api.Tests;

public sealed class ProductSearchEngineTests
{
    private sealed record TestItem(Guid Id, string Name, string Sku, string? Description);

    [Fact]
    public void Search_FuzzyMatch_FindsLaptop_WhenQueryHasTypos()
    {
        var engine = new ProductSearchEngine();

        var laptop = new TestItem(Guid.NewGuid(), "Laptop Pro 14", "LTP-014-PRO", "High performance laptop");
        var mouse = new TestItem(Guid.NewGuid(), "Wireless Mouse", "MSE-WRL-001", "Ergonomic mouse");

        var items = new[] { mouse, laptop };

        var results = engine.Search(
            items,
            query: "lptop",
            maxResults: 10,
            fields: i => new (string Text, int Weight)[]
            {
                (i.Name, 5),
                (i.Sku, 4),
                (i.Description ?? string.Empty, 1)
            },
            getId: i => i.Id);

        Assert.Contains(laptop, results);
        Assert.DoesNotContain(mouse, results);
    }

    [Fact]
    public void Search_WeightedFields_PrefersSkuMatch_WhenSkuWeightIsHigher()
    {
        var engine = new ProductSearchEngine();

        var skuMatch = new TestItem(Guid.NewGuid(), "Something Else", "ABC-123", null);
        var nameMatch = new TestItem(Guid.NewGuid(), "ABC 123", "NOPE", null);

        var items = new[] { nameMatch, skuMatch };

        var results = engine.Search(
            items,
            query: "abc-123",
            maxResults: 10,
            fields: i => new (string Text, int Weight)[]
            {
                (i.Name, 1),
                (i.Sku, 10)
            },
            getId: i => i.Id);

        Assert.Equal(skuMatch, results.First());
    }

    [Fact]
    public void Search_CachesResultsByNormalizedQuery()
    {
        var engine = new ProductSearchEngine();

        var item = new TestItem(Guid.NewGuid(), "Laptop", "LTP", null);
        var items = new[] { item };

        _ = engine.Search(
            items,
            query: "  LAPTOP ",
            maxResults: 10,
            fields: i => new (string Text, int Weight)[] { (i.Name, 1) },
            getId: i => i.Id);

        var cache = ReadCache(engine);

        Assert.True(cache.ContainsKey("laptop"));
        Assert.Contains(item.Id, cache["laptop"]);
    }

    private static Dictionary<string, IReadOnlyList<Guid>> ReadCache(ProductSearchEngine engine)
    {
        var field = typeof(ProductSearchEngine).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);

        var value = field!.GetValue(engine);
        Assert.NotNull(value);

        return Assert.IsType<Dictionary<string, IReadOnlyList<Guid>>>(value);
    }
}
