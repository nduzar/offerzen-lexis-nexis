using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Domain;
using ProductCatalog.Api.Dtos;
using ProductCatalog.Api.Repositories;

namespace ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryRepository categories) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(CancellationToken cancellationToken)
    {
        var items = await categories.ListAsync(cancellationToken);
        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("tree")]
    public async Task<ActionResult<IReadOnlyList<CategoryTreeNodeDto>>> Tree(CancellationToken cancellationToken)
    {
        var tree = await categories.GetTreeAsync(cancellationToken);
        return Ok(tree.Select(ToTreeDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name!,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId
        };

        await categories.AddAsync(category, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = category.Id }, ToDto(category));
    }

    private static CategoryDto ToDto(Category category)
    {
        return new CategoryDto(category.Id, category.Name, category.Description, category.ParentCategoryId);
    }

    private static CategoryTreeNodeDto ToTreeDto(CategoryNode node)
    {
        return new CategoryTreeNodeDto(
            node.Category.Id,
            node.Category.Name,
            node.Category.Description,
            node.Category.ParentCategoryId,
            node.Children.Select(ToTreeDto).ToList());
    }

    private ActionResult? Validate(CreateCategoryRequest request)
    {
        return request switch
        {
            { Name: null or "" } => BadRequest(new { error = "name_required" }),
            _ => null
        };
    }
}
