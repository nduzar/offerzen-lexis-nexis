# Product Catalog Management System (C# + Angular)

This repository contains:

- **Backend**: ASP.NET Core Web API (`backend/ProductCatalog.Api`)
- **Frontend**: Angular SPA (`frontend/`)

## Prerequisites

- .NET SDK **9.x**
- Node.js **18+** (installed)

On Windows PowerShell, if your execution policy blocks `npm`, use `npm.cmd` (this repo’s scripts assume `npm.cmd` works).

## Backend (ASP.NET Core)

### Run

From the repo root:

```powershell
dotnet run --project backend/ProductCatalog.Api/ProductCatalog.Api.csproj
```

The API will listen on a localhost port defined in `backend/ProductCatalog.Api/Properties/launchSettings.json`.

Swagger UI:

- `http://localhost:5012/swagger`

### Test

```powershell
dotnet test ProductCatalog.sln
```

## Frontend (Angular)

### Install

```powershell
cd frontend
npm.cmd install
```

### Run

```powershell
cd frontend
npm.cmd start
```

Frontend runs at:

- `http://localhost:4200`

### Test

```powershell
cd frontend
npm.cmd test -- --watch=false
```

## API Overview

- **Products**
  - `GET /api/products` (pagination + filters + fuzzy search)
  - `GET /api/products/{id}`
  - `POST /api/products`
  - `PUT /api/products/{id}`
  - `DELETE /api/products/{id}`
  - `POST /api/products/manual` (manual model binding example)
  - `GET /api/products/{id}/legacy` (custom JSON serialization example)

- **Categories**
  - `GET /api/categories` (flat list)
  - `GET /api/categories/tree` (hierarchical)
  - `POST /api/categories`

## Interesting features (where to find them)

This section is designed for quick code review. Each item includes a file path and line ranges.

### Frontend (Angular)

#### Products list: reactive stream + safe loading

- **File**: `frontend/src/app/pages/product-list.page.ts`
- **UI template**: lines **13–86**
- **Reactive data flow (`vm$`)**: lines **204–228**
- **Why it’s interesting**:
  - Uses `switchMap` so “latest request wins” (prevents race conditions when filters change quickly).
  - Uses `finalize` to reliably reset `loading`.
- **Trade-off**:
  - Stream-driven UIs are clean, but you must avoid accidentally unsubscribing during loading states.

#### Add/Edit product: reactive form + validation

- **File**: `frontend/src/app/pages/product-form.page.ts`
- **Form template + hints**: lines **26–73**
- **Form model + validators**: lines **166–173**
- **Submit flow (create/update + error handling)**: lines **210–245**
- **Why it’s interesting**:
  - Reactive forms give explicit state and validation.
  - Client-side validation improves UX, and server-side validation remains the source of truth.
- **Trade-off**:
  - Some validation rules exist in both frontend and backend (common in real apps).

#### Categories: flat list + hierarchical tree renderer

- **File**: `frontend/src/app/pages/categories.page.ts`
- **Recursive tree component**: lines **9–35**
- **Page template (create + tree + flat list)**: lines **41–114**
- **Reload (flat list + tree endpoints)**: lines **257–272**
- **Why it’s interesting**:
  - The tree is rendered using a small recursive component for clarity.
- **Trade-off**:
  - For extremely large/deep trees, you’d consider caching/virtualization.

#### Standalone routes (lazy load)

- **File**: `frontend/src/app/app.routes.ts`
- **Routes**: lines **3–24**
- **Why it’s interesting**:
  - Standalone components are lazy-loaded via `loadComponent`, keeping the setup simple.

#### Typed API service (single HTTP integration point)

- **File**: `frontend/src/app/core/catalog-api.service.ts`
- **Endpoints used by UI**: lines **20–65**
- **Trade-off**:
  - `API_BASE_URL` is a simple constant (take-home speed). Production would typically use environment config or an injection token.

### Backend (Swagger + endpoints)

#### Swagger + CORS dev experience

- **File**: `backend/ProductCatalog.Api/Program.cs`
- **CORS policy**: lines **7–16**
- **HTTPS redirection (disabled in Development)**: lines **36–39**

#### Products endpoint highlights

- **File**: `backend/ProductCatalog.Api/Controllers/ProductsController.cs`
- **List endpoint (paging + search path)**: lines **16–70**
- **Create endpoint + `201 Created`**: lines **79–111**
- **Pattern-matching validation**: lines **210–238**
- **Trade-off**:
  - In-memory search is fine at take-home scale; for large catalogs you’d push search to indexed storage (DB full-text or a search service).

#### Categories endpoint highlights

- **File**: `backend/ProductCatalog.Api/Controllers/CategoriesController.cs`
- **Flat list + tree endpoints**: lines **12–24**
- **Create endpoint**: lines **26–45**
- **Trade-off**:
  - Server-side tree is easy for the UI; for large trees you might cache it.

## Notes

- The backend seeds a few products on startup for demo purposes.
- See `SOLUTION.md` for design decisions and how the assignment requirements were implemented.