using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Domain;
using ProductCatalog.Api.Dtos;
using ProductCatalog.Api.Repositories;
using ProductCatalog.Api.Search;

namespace ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    IProductRepository products,
    ProductSearchEngine searchEngine) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductSearchResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? name = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var all = await products.ListAsync(cancellationToken);
            var matches = searchEngine.Search(
                all,
                search,
                maxResults: 200,
                fields: p => new (string Text, int Weight)[]
                {
                    (p.Name, 5),
                    (p.Sku, 4),
                    (p.Description ?? string.Empty, 1)
                },
                getId: p => p.Id);

            var filtered = matches.AsEnumerable();

            if (categoryId is Guid catId)
            {
                filtered = filtered.Where(p => p.CategoryId == catId);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                filtered = filtered.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }

            var total = filtered.Count();
            var pageItems = filtered
                .OrderBy(p => p)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDto)
                .ToList();

            return Ok(new ProductSearchResponse(pageItems, total, page, pageSize));
        }

        var items = await products.ListPagedAsync(page, pageSize, categoryId, name, cancellationToken);
        var totalCount = await products.CountAsync(categoryId, name, cancellationToken);

        return Ok(new ProductSearchResponse(items.Select(ToDto).ToList(), totalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(ToDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
        {
            return validation;
        }

        var existing = await products.GetBySkuAsync(request.Sku!, cancellationToken);
        if (existing is not null)
        {
            return Conflict(new { error = "sku_already_exists" });
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name!,
            Description = request.Description,
            Sku = request.Sku!,
            Price = request.Price!.Value,
            Quantity = request.Quantity!.Value,
            CategoryId = request.CategoryId!.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        await products.AddAsync(product, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = product.Id }, ToDto(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var validation = ValidateUpdate(request);
        if (validation is not null)
        {
            return validation;
        }

        product.Name = request.Name!;
        product.Description = request.Description;
        product.Sku = request.Sku!;
        product.Price = request.Price!.Value;
        product.Quantity = request.Quantity!.Value;
        product.CategoryId = request.CategoryId!.Value;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await products.UpdateAsync(product, cancellationToken);
        return Ok(ToDto(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await products.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("manual")]
    public async Task<ActionResult<ProductDto>> CreateManual(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.Deserialize<CreateProductRequest>(body, options);

        if (request is null)
        {
            return BadRequest(new { error = "invalid_json" });
        }

        return await Create(request, cancellationToken);
    }

    [HttpGet("{id:guid}/legacy")]
    public async Task GetLegacy(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(id, cancellationToken);
        if (product is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.ContentType = "application/json";

        var payload = new
        {
            product.Id,
            product.Sku,
            Name = product.Name,
            Price = product.Price,
            product.Quantity,
            product.CreatedAt,
            product.UpdatedAt
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        await JsonSerializer.SerializeAsync(Response.Body, payload, options, cancellationToken);
    }

    private static ProductDto ToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.Price,
            product.Quantity,
            product.CategoryId,
            product.CreatedAt,
            product.UpdatedAt);
    }

    private ActionResult? ValidateCreate(CreateProductRequest request)
    {
        return request switch
        {
            { Name: null or "" } => BadRequest(new { error = "name_required" }),
            { Sku: null or "" } => BadRequest(new { error = "sku_required" }),
            { CategoryId: null } => BadRequest(new { error = "category_required" }),
            { Price: null } => BadRequest(new { error = "price_required" }),
            { Price: <= 0 } => BadRequest(new { error = "price_must_be_positive" }),
            { Quantity: null } => BadRequest(new { error = "quantity_required" }),
            { Quantity: < 0 } => BadRequest(new { error = "quantity_cannot_be_negative" }),
            _ => null
        };
    }

    private ActionResult? ValidateUpdate(UpdateProductRequest request)
    {
        return request switch
        {
            { Name: null or "" } => BadRequest(new { error = "name_required" }),
            { Sku: null or "" } => BadRequest(new { error = "sku_required" }),
            { CategoryId: null } => BadRequest(new { error = "category_required" }),
            { Price: null } => BadRequest(new { error = "price_required" }),
            { Price: <= 0 } => BadRequest(new { error = "price_must_be_positive" }),
            { Quantity: null } => BadRequest(new { error = "quantity_required" }),
            { Quantity: < 0 } => BadRequest(new { error = "quantity_cannot_be_negative" }),
            _ => null
        };
    }
}
