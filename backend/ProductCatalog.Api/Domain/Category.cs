namespace ProductCatalog.Api.Domain;

public sealed class Category
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public Guid? ParentCategoryId { get; set; }
}
