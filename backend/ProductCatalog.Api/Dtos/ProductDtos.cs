namespace ProductCatalog.Api.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    int Quantity,
    Guid CategoryId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateProductRequest(
    string? Name,
    string? Description,
    string? Sku,
    decimal? Price,
    int? Quantity,
    Guid? CategoryId);

public sealed record UpdateProductRequest(
    string? Name,
    string? Description,
    string? Sku,
    decimal? Price,
    int? Quantity,
    Guid? CategoryId);

public sealed record ProductSearchResponse(
    IReadOnlyList<ProductDto> Items,
    int Total,
    int Page,
    int PageSize);
