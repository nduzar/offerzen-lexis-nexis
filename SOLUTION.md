# SOLUTION

## Overview
This solution implements a **Product Catalog Management System** with:

- **ASP.NET Core Web API** backend (products + categories + search)
- **Angular SPA** frontend (product list/search/filter, create/edit, delete confirmation, and category management)

The focus is on demonstrating senior-level C# and TypeScript patterns while keeping the UI deliberately simple.

## Backend design

### Project structure
- `Domain/`
  - `Product` and `Category` entities
- `Dtos/`
  - API DTOs as **C# record types**
- `Repositories/`
  - Generic repository base + product/category repositories
- `Search/`
  - LINQ extension methods + `ProductSearchEngine`
- `Middleware/`
  - Custom middleware (built from scratch)
- `Extensions/`
  - DI registration + database seeding

### Repository pattern (generics + interfaces)
- `IRepository<TEntity, TKey>` and `Repository<TEntity, TKey>` provide a reusable base.
- `ProductRepository` uses **EF Core InMemory** for most persistence.
- `InMemoryCategoryRepository` uses **pure in-memory collections** (`Dictionary<Guid, Category>`) and locks for thread-safety.

This satisfies the requirement of implementing a custom generic repository and having at least one repository not backed by EF.

### Nullable reference types
The backend uses `<Nullable>enable</Nullable>` and models/DTOs are designed accordingly (e.g., `string? Description`).

### DTOs as record types
All request/response DTOs are implemented as **records** (C# 9+), e.g. `CreateProductRequest`, `ProductDto`.

### Pattern matching validation
Request validation uses C# pattern matching:

- `CreateProductRequest` and `UpdateProductRequest` validation uses `switch` expressions with property patterns.

### Custom LINQ extension methods
`Search/ProductQueryExtensions.cs` demonstrates custom LINQ extensions used by the product repository:

- `ApplyCatalogFilters(...)`
- `WhereNameContains(...)`
- `WhereHasInventory()`

### Category tree (hierarchical categories)
Categories support parent-child relationships (`ParentCategoryId`).

- `GET /api/categories/tree` returns a hierarchical structure built from the in-memory repository using a tree node record (`CategoryNode`).

### Sorting with IComparable
`Product` implements `IComparable<Product>` so the repository and API can apply a consistent ordering:

- Primary: `Name` (case-insensitive)
- Secondary: `Price`

### ProductSearchEngine (core C# only)
`Search/ProductSearchEngine.cs` is implemented using only the .NET Base Class Library.

Capabilities:
- **Generic** search: `Search<T>` accepts field selectors and id selectors.
- **Weighted scoring**: callers provide multiple fields with weights.
- **Fuzzy matching**: Levenshtein-based similarity.
- **Token-aware scoring**: fuzzy similarity checks the full field text *and* individual tokens.

### Caching layer for search results
`ProductSearchEngine` caches query results using a `Dictionary<string, IReadOnlyList<Guid>>`.

- Cache key: normalized query string
- Cache value: entity IDs in score order

### Custom middleware (from scratch)
Two custom middleware components:

- `ExceptionHandlingMiddleware`
  - Converts unhandled exceptions into a JSON error response.
- `RequestCorrelationMiddleware`
  - Implements `X-Correlation-Id` propagation.

### Manual model binding
`POST /api/products/manual` demonstrates manual binding by reading the request body and deserializing with `JsonSerializer`.

### Custom JSON serialization
`GET /api/products/{id}/legacy` demonstrates custom serialization options (indented JSON, no naming policy).

### DI usage
`Extensions/ServiceCollectionExtensions.cs` registers:

- `AppDbContext` (EF InMemory)
- `IProductRepository` / `ProductRepository`
- `ICategoryRepository` / `InMemoryCategoryRepository`
- `ProductSearchEngine`

## Backend tests
A dedicated xUnit project validates the “core C# challenge” portion:

- Fuzzy matching handles typos (e.g., `lptop` matches `Laptop Pro 14`).
- Weighted fields prefer higher-weight field matches.
- Cache population is validated via reflection.

Run:

```powershell
dotnet test ProductCatalog.sln
```

## Frontend design (Angular)

### Standalone components and routing
The frontend uses standalone components and lazy-loaded routes:

- `/products` list + search/filter + paging
- `/products/new` add
- `/products/:id` edit
- `/categories` manage categories + tree

### Strong typing
All API contracts are represented with TypeScript interfaces in `src/app/core/models.ts`.

### RxJS patterns
- API calls are centralized in `CatalogApiService`.
- `ProductListPage` uses a refresh Subject + RxJS operators to load data and manage loading/error state.

### Reactive forms with validation
`ProductFormPage` uses reactive forms with validators:

- required: name, sku, category
- numeric: price > 0, quantity >= 0

### Error handling and feedback
Pages render:

- loading state
- error state
- confirmation dialog on delete

### Frontend unit test
`app.spec.ts` was updated to match the new shell and validates the app renders successfully.

Run:

```powershell
cd frontend
npm.cmd test -- --watch=false
```

## Trade-offs / simplifications
- Category management focuses on create + read and tree visualization (no edit/delete UI).
- In-memory persistence is used to keep setup trivial and reviewer-friendly.
- Validation is intentionally lightweight and uses pattern matching to satisfy the requirement (FluentValidation could be added if desired).

## How to demo (suggested flow)
- Start backend (`dotnet run ...`), open Swagger.
- Start frontend (`npm.cmd start`).
- Show:
  - product list + fuzzy search
  - filter by category
  - create/edit/delete
  - categories page (flat list + tree)
  - mention middleware correlation id + legacy JSON endpoint
