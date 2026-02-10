# ndu-prep (Interview Prep)

You’ll convert this Markdown to `ndu-prep.pdf` locally. The repo already ignores `*.pdf` and explicitly ignores `ndu-prep.pdf`.

---

## 1) What you built (high-level)

### Goal
A **Product Catalog Management System** with:

- A **C# ASP.NET Core Web API** backend that supports:
  - CRUD operations for products
  - Category management (including hierarchical category trees)
  - Search, filtering, pagination
  - “Senior concepts” from the assignment: middleware, DI, caching, fuzzy matching, generics, etc.

- An **Angular SPA** frontend that supports:
  - Viewing and searching products
  - Creating/updating/deleting products
  - Viewing categories (flat + tree) and creating categories

### Why this is a good take-home
It demonstrates:

- You can design an API and UI that match each other
- You can implement **non-trivial** requirements cleanly
- You understand fundamentals (no magic frameworks required)
- You can trade off complexity vs. time

---

## 2) How to run the solution (what you’ll say in the interview)

### Backend
From repo root:

```powershell
dotnet run --project backend/ProductCatalog.Api/ProductCatalog.Api.csproj
```

- Swagger: `http://localhost:5012/swagger`

### Frontend
From `frontend/`:

```powershell
npm.cmd install
npm.cmd start
```

- App: `http://localhost:4200`

### Tests

Backend:

```powershell
dotnet test ProductCatalog.sln
```

Frontend:

```powershell
cd frontend
npm.cmd test -- --watch=false
```

Interview tip: always mention you included tests and they pass.

---

## 3) Repository structure (how to talk about it)

At the root:

- `backend/ProductCatalog.Api` — ASP.NET Core API
- `backend/ProductCatalog.Api.Tests` — xUnit tests
- `frontend/` — Angular SPA
- `README.md` — run instructions
- `SOLUTION.md` — design write-up

### Backend folder structure (what each folder is responsible for)

- `Domain/`
  - Entities: `Product`, `Category`
  - Domain behavior: `Product` implements `IComparable<Product>` (sorting)

- `Dtos/`
  - Records that model API requests + responses
  - Example: `CreateProductRequest`, `ProductDto`, `CategoryDto`, etc.

- `Data/`
  - EF Core `AppDbContext` (in-memory DB)

- `Repositories/`
  - `IRepository<TEntity, TKey>` and `Repository<TEntity, TKey>` generic base
  - `ProductRepository` (EF-based)
  - `InMemoryCategoryRepository` (pure in-memory)

- `Search/`
  - `ProductQueryExtensions` (custom LINQ extensions)
  - `ProductSearchEngine` (weighted fuzzy search + caching)

- `Middleware/`
  - `ExceptionHandlingMiddleware` (global error handling)
  - `RequestCorrelationMiddleware` (correlation IDs)

- `Extensions/`
  - `AddProductCatalog()` registration
  - `SeedAsync()` database seed data

Interview tip: This is a “modular monolith” structure inside one project.

### Frontend folder structure

- `src/app/core/`
  - API config, models, and `CatalogApiService`

- `src/app/pages/`
  - Standalone pages:
    - `product-list.page.ts`
    - `product-form.page.ts`
    - `categories.page.ts`

- `src/app/app.routes.ts`
  - Lazy routes

- `src/app/app.html`
  - Shell layout (header + router outlet)

---

## 4) Backend: Key concepts and how to explain them

### 4.1 ASP.NET Core pipeline (request flow)

When a request hits the backend:

1. ASP.NET creates an `HttpContext`
2. Middleware runs in order
3. Routing selects a controller action
4. Model binding + validation happen (depending on how you implement)
5. Controller calls repo/services
6. Response is returned

In your app:

- `ExceptionHandlingMiddleware`
  - catches unhandled exceptions and returns JSON

- `RequestCorrelationMiddleware`
  - adds `X-Correlation-Id` to response and stores it in context

Then:

- Controllers handle the request

Interview Q: “Why custom middleware instead of exception filters?”

Good answer:

- Middleware is cross-cutting and applies to all endpoints
- It centralizes error handling and correlation consistently
- Filters are also valid; middleware is simpler to demonstrate

### 4.2 Dependency Injection (DI)

In `Program.cs` you register services:

- `AddControllers()`
- `AddSwaggerGen()`
- `AddProductCatalog()`

In `AddProductCatalog()`:

- EF Core `AppDbContext` (scoped)
- Repositories
- `ProductSearchEngine` (singleton)

Interview Q: “Why is `ProductSearchEngine` singleton?”

- It has an internal cache dictionary; singleton allows reuse
- Its behavior is stateless except for caching

Trade-off:

- Must ensure thread-safety (you did with a lock)

### 4.3 EF Core InMemory DB

You used:

```csharp
UseInMemoryDatabase("ProductCatalog")
```

Why:

- Fast to set up
- No external dependencies
- Great for take-home

Trade-offs:

- Not production realistic (no persistence)
- Different behavior vs relational DB (constraints, SQL translation, etc.)

Interview Q: “How would you move to SQL Server/Postgres?”

- Replace provider to `UseSqlServer` / `UseNpgsql`
- Add migrations
- Ensure indexes/constraints
- Handle concurrency tokens

### 4.4 Generic Repository base (why it exists)

`IRepository<TEntity, TKey>` and `Repository<TEntity, TKey>` show:

- You can write reusable abstractions
- You understand generics and constraints

But also mention:

- Repositories can be over-abstracted
- EF already acts like a repository (DbSet)

Your trade-off:

- You used a light abstraction to satisfy assignment and keep code clean

### 4.5 In-memory Category repository (assignment requirement)

`InMemoryCategoryRepository` uses:

- `Dictionary<Guid, Category>`
- `lock` for thread-safety

Trade-offs:

- Locking is simple and safe
- Could use `ConcurrentDictionary`, but tree operations still need consistent snapshots

Interview Q: “Why not store categories in EF too?”

- The assignment asked for at least one non-EF repo
- Also shows you can manage in-memory state carefully

### 4.6 Records for DTOs

You used `record` for DTOs:

- Immutability by default
- Value-based equality
- Concise

Trade-off:

- Sometimes you need mutable DTOs for binding
- But here the controller uses explicit validation and mapping

### 4.7 Nullable reference types

You have `<Nullable>enable</Nullable>` and use:

- `string? Description`

Why:

- Safer null handling
- Catch null bugs at compile time

Interview Q: “What’s the difference between `string` and `string?`?”

- `string` should never be null
- `string?` can be null and the compiler enforces checks

### 4.8 Pattern matching validation

Your controllers use:

```csharp
return request switch
{
  { Name: null or "" } => BadRequest(...),
  ...
};
```

Why:

- Very readable for small validation rules
- Shows modern C# feature knowledge

Trade-off:

- For bigger validation rules you’d use:
  - FluentValidation
  - DataAnnotations
  - custom validator services

### 4.9 Custom LINQ extensions

`ProductQueryExtensions` demonstrates:

- Extension methods
- Building reusable query logic

You used:

- `ApplyCatalogFilters(categoryId, name)`

Trade-off:

- Must be careful with EF translation
- You used `.Contains(..., StringComparison.OrdinalIgnoreCase)` which may not translate for relational providers

If asked:

- For real DB, use `EF.Functions.Like()` or normalize/collation

### 4.10 Search engine: weighted fuzzy matching + caching

`ProductSearchEngine` is the “core C# challenge” part.

Features:

- Generic method `Search<T>` with:
  - `items` enumerable
  - `fields` selector returning `(Text, Weight)` tuples
  - `getId` selector

Scoring logic:

- If the field contains query substring: big score
- Else compute Levenshtein similarity
- Token-aware: compares query to each token in the field

Caching:

- Key: normalized query
- Value: list of matching item IDs

Trade-off discussion:

- Cache has no eviction (fine for take-home)
- For production, consider:
  - Memory limits
  - LRU eviction
  - `IMemoryCache`

Interview Q: “Why Levenshtein?”

- Simple and deterministic
- Good for typos
- Works without external libraries

### 4.11 `IComparable<Product>` sorting

Your `Product.CompareTo` sorts by:

- Name (case-insensitive)
- Price

Why:

- Consistent ordering across the system
- Easier to reason about

Trade-off:

- Hard-coded sort criteria
- Could implement API sort parameters

### 4.12 Manual model binding

`POST /api/products/manual` reads body:

- `StreamReader(Request.Body)`
- `JsonSerializer.Deserialize<CreateProductRequest>`

Why:

- Demonstrates understanding of the request pipeline
- Shows you can implement binding manually

Trade-off:

- Normal model binding is preferred
- Manual binding needs careful validation/error handling

### 4.13 Custom JSON serialization

`GET /api/products/{id}/legacy`:

- Writes a JSON payload with custom options
- Example: `PropertyNamingPolicy = null` and `WriteIndented = true`

Why:

- Shows you can customize JSON output

Trade-off:

- Usually you configure `JsonOptions` globally
- Here it’s endpoint-specific by design

---

## 5) Backend: API design & endpoint behavior

### Products

- `GET /api/products`
  - Query params:
    - `page`, `pageSize`
    - `categoryId`
    - `name`
    - `search` (fuzzy)

Two modes:

- If `search` is present, it loads all products and uses `ProductSearchEngine`
- Otherwise, it uses repository paging

Trade-off:

- “Load all then search” doesn’t scale
- For take-home it’s OK
- For production you’d implement:
  - full-text search (Elastic/Lucene)
  - database search with indexes

### Categories

- `GET /api/categories/tree` builds nested nodes

Trade-off:

- For very large trees, you’d use caching or store adjacency list + materialized path

---

## 6) Backend: Testing strategy

What you tested:

- The most “logic-heavy” part: `ProductSearchEngine`

Tests include:

- Fuzzy matching handles typos
- Weighting influences ordering
- Cache dictionary gets populated

Trade-off:

- Reflection in tests is not ideal
- But acceptable to validate internal caching requirement for take-home

If asked:

- In production, expose cache stats via interface or metrics rather than reflection

---

## 7) Angular: Key concepts and how to explain them

### 7.1 Angular standalone components

Your pages are `standalone: true`, meaning:

- No `NgModule` required
- You import dependencies directly

Why:

- Modern Angular
- Less boilerplate

Trade-off:

- Teams used to modules may find it new

### 7.2 Routing

`app.routes.ts` uses lazy `loadComponent`:

- faster initial load
- clear separation of pages

### 7.3 HttpClient + API service layer

`CatalogApiService`:

- Central place for API calls
- Typed request/response

Why:

- Avoid duplicating URLs/params
- Easier to change base URL

Trade-off:

- Could use interceptors for auth/logging
- Not needed in this take-home

### 7.4 RxJS usage patterns

In `ProductListPage`:

- A `Subject<void>` triggers refresh
- RxJS pipeline manages:
  - loading state
  - error state
  - fetching data

Why:

- Demonstrates you can build reactive flows

Trade-off:

- For bigger apps, you might use:
  - NgRx
  - signals store pattern

### 7.5 Reactive forms

In `ProductFormPage`:

- Strong validators:
  - required
  - min values

Why:

- Prevent invalid submissions
- Good UX

Trade-off:

- Still rely on server validation as source of truth

### 7.6 UI trade-offs

You chose:

- A simple but clean UI without external UI libs

Why:

- Keep dependencies low
- Focus on correctness and architecture

If asked:

- In production, you’d adopt a design system (Material, Tailwind, etc.)

---

## 8) Cross-cutting concerns to mention

### Error handling

Backend:

- Middleware returns consistent JSON for unhandled exceptions

Frontend:

- Pages show `error()` state

### Correlation IDs

- Helps trace logs across services
- Important in distributed systems

### CORS

- Frontend origin allowed during dev

Trade-off:

- In production you’d configure allowed origins per environment

---

## 9) “Senior” trade-offs & what interviewers probe

### 9.1 Why not over-engineer?

A good take-home solution:

- meets requirements
- is easy to review
- avoids unnecessary complexity

Explain:

- You kept things simple (in-memory DB) but structured code cleanly

### 9.2 Scalability

What wouldn’t scale:

- In-memory DB
- In-memory category repo
- “Load all products to search”

How to scale:

- persistent DB
- indexes
- full-text search
- caching with eviction

### 9.3 API contracts

You used DTOs rather than exposing EF entities directly.

Why:

- decoupling
- avoid leaking internal schema

### 9.4 Validation

Pattern matching is fine now.

In production:

- centralize validation
- return standard problem details (`ProblemDetails`)

### 9.5 Logging

You have middleware-based exception logging.

Next step:

- structured logs (Serilog)
- correlation IDs included in log scopes

---

## 10) Common interview questions + strong answers

### Q: “Explain the request flow end-to-end.”

Answer outline:

- request -> middleware -> controller -> repo -> DB -> response
- mention correlation id + exception middleware

### Q: “Why did you choose EF InMemory?”

- time-to-deliver
- no external dependency
- good for take-home

Then explain what you would do for production.

### Q: “Where does business logic live?”

- In this take-home, logic is mostly in controllers + repositories + search engine
- For larger apps, you’d add a `Services/` layer or domain services

### Q: “How would you handle concurrency?”

- Add row-version / concurrency tokens
- Use optimistic concurrency in EF

### Q: “How would you secure the API?”

- JWT auth
- RBAC
- input validation
- rate limiting
- CORS hardening

### Q: “How would you improve search?”

- database indexes
- full-text search engine
- store precomputed tokens

### Q: “What would you do differently with more time?”

- more tests (integration tests)
- shared validation layer
- pagination + sorting options
- category edit/delete
- better UI components

---

## 11) Quick mental checklist for the interview

Before the interview:

- You can run backend + frontend live
- You can show Swagger
- You can show product search working (typo query)
- You can explain:
  - why certain choices were made
  - what you’d do in production

During the interview:

- Be honest about trade-offs
- Always mention “what next” improvements
- Speak clearly about responsibilities per layer

---

## 12) Glossary (junior-friendly)

- **DTO**: A “shape” of data sent/received over the API. Keeps API separate from DB model.
- **DI**: The framework creates objects for you and injects dependencies.
- **Middleware**: Code that runs on every request before controllers.
- **Repository**: Abstraction for data access.
- **LINQ**: Query and transform collections/data.
- **RxJS**: Reactive programming library used in Angular.
- **Reactive forms**: Angular way to build forms with validation and value tracking.
- **CORS**: Browser security rule for cross-domain requests.

---

## 13) What to print / convert to PDF

Convert this file to `ndu-prep.pdf`. It’s gitignored.
