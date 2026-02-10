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

## Notes

- The backend seeds a few products on startup for demo purposes.
- See `SOLUTION.md` for design decisions and how the assignment requirements were implemented.