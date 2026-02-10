namespace ProductCatalog.Api.Dtos;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId);

public sealed record CreateCategoryRequest(
    string? Name,
    string? Description,
    Guid? ParentCategoryId);

public sealed record CategoryTreeNodeDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    IReadOnlyList<CategoryTreeNodeDto> Children);
