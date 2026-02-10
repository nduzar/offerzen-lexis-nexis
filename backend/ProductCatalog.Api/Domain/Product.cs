namespace ProductCatalog.Api.Domain;

public sealed class Product : IComparable<Product>
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string Sku { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public Guid CategoryId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int CompareTo(Product? other)
    {
        if (other is null)
        {
            return 1;
        }

        var nameCompare = string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        if (nameCompare != 0)
        {
            return nameCompare;
        }

        return Price.CompareTo(other.Price);
    }
}
