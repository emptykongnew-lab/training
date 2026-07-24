# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

OrderHub is a small ASP.NET Core MVC (.NET 8) training application for managing customers, products, and orders, backed by EF Core / SQL Server.

## Commands

```bash
# Restore & build
dotnet build

# Run the web app (applies EF migrations and seeds data automatically on startup)
dotnet run --project src/OrderHub.Web

# Run all tests
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~OrderServiceCreateTests"
dotnet test --filter "FullyQualifiedName~OrderServiceCreateTests.CreateOrder_InsufficientStock_FailsWithMessage"

# EF Core migrations (run from repo root; -s points at the startup project)
dotnet ef migrations add <Name> -p src/OrderHub.Infrastructure -s src/OrderHub.Web
dotnet ef database update -p src/OrderHub.Infrastructure -s src/OrderHub.Web
```

Tests use `Microsoft.EntityFrameworkCore.InMemory`, so they don't require a real SQL Server instance. The web app itself requires SQL Server — the connection string is `ConnectionStrings:Default` in `src/OrderHub.Web/appsettings.json` / `appsettings.Development.json`.

## Architecture

Three-project layered structure, referenced as `Web -> Core` and `Web/Infrastructure -> Core`, with `Infrastructure` implementing `Core`'s interfaces:

- **`src/OrderHub.Core`** — domain layer, no EF Core dependency.
  - `Domain/` — POCO entities: `Customer`, `Product`, `Order`, `OrderItem`, plus enums `CustomerTier` (Standard/Silver/Gold) and `OrderStatus` (Pending/Confirmed/Shipped/Cancelled).
  - `Interfaces/` — repository contracts (`ICustomerRepository`, `IProductRepository`, `IOrderRepository`) implemented in `Infrastructure`.
  - `Services/` — business logic (`OrderService`, `ProductService`, `CustomerService`) implementing `I*Service` interfaces; these are what controllers depend on, never repositories directly.
  - `Common/` — `ServiceResult<T>` (success/failure + error messages, used as the return type for mutating service calls) and `PagedResult<T>` (paging wrapper used for list queries).
- **`src/OrderHub.Infrastructure`** — EF Core implementation.
  - `Data/OrderHubDbContext.cs` — entity configuration (relationships, precision, indexes, cascade rules) lives here in `OnModelCreating`, not via separate `IEntityTypeConfiguration` classes.
  - `Data/DbSeeder.cs` — idempotent (skips if any customers exist) seed data generator using a fixed `Random` seed for reproducibility; produces 20 customers, 50 products (including some low-stock/inactive), 200 orders across all statuses.
  - `Migrations/` — standard EF Core migrations.
  - `Repositories/` — thin EF Core-backed implementations of the `Core` repository interfaces.
- **`src/OrderHub.Web`** — ASP.NET Core MVC front end. Standard `Controllers/` + `Views/` (Razor, Bootstrap) + `ViewModels/` (one per view, mapped manually in controllers — no AutoMapper). `Helpers/DisplayHelper.cs` centralizes UI label/formatting logic (status labels/badge classes, tier labels, currency/date formatting) — use it instead of duplicating switch statements in views.
- **`tests/OrderHub.Tests`** — xUnit tests for the `Core` services only (no controller/integration tests). `TestSetup.cs` is the shared fixture: `CreateContext()` gives an EF Core InMemory `OrderHubDbContext` (new GUID-named DB per call), plus factory helpers (`CreateOrderService`, `AddCustomer`, `AddProduct`) for building test data. Test files are one-per-scenario-group (e.g. `OrderServiceCreateTests`, `OrderServiceCancelTests`, `OrderServicePricingTests`, `OrderServiceQueryTests`).

### DI wiring

All service/repository registrations happen in `src/OrderHub.Web/Program.cs` — each repository and service is registered as `Scoped`. When adding a new entity, follow the existing pattern: repository interface + implementation, service interface + implementation, register both in `Program.cs`.

### Business rules worth knowing

- Order pricing: `OrderService.CalculateTotal` applies a per-tier discount (Gold 10%, Silver 5%, Standard 0%) to the subtotal of `UnitPriceSnapshot * Quantity` across items. Note that in `CreateOrderAsync`, the tier discount is only pre-applied to the snapshotted unit price for Gold customers; `CalculateTotal` re-applies the tier discount rate on top when rendering totals.
- `CreateOrderAsync` validates customer existence, non-empty lines, positive quantities, no duplicate products per order, product existence/active status, and stock sufficiency — collecting all line-level errors before failing (partial success is not allowed; nothing is persisted if any error exists).
- `CancelOrderAsync` only allows cancelling orders in `Pending` or `Confirmed` status, and restores stock quantities to the related products on cancellation.
- Mutating service methods return `ServiceResult<T>` (check `.Success`, use `.ErrorMessage` for the combined error string, or `.Errors` for the list); query methods return the value directly (`Order?`, `PagedResult<T>`, etc.).

### Localization note

Domain data (seed names/emails), validation/error messages, and UI labels throughout the codebase are in Traditional Chinese (zh-TW). Match this convention for any new user-facing strings or seed data rather than switching to English.

## Important / dangerous files

- `src/OrderHub.Infrastructure/Migrations/**` — EF Core migrations are historical records; do not hand-edit existing migration files.
- `src/OrderHub.Web/appsettings.json` / `appsettings.Development.json` — contain connection strings; confirm with the user before changing.
- Never read or write secret files (`*.pfx`, `appsettings.Production.json`, user-secrets).

## Don'ts

- Don't add new NuGet packages without asking first.
- Don't access `DbContext` directly from Controllers or Views — only repositories may touch it.
- Don't refactor unrelated code "while you're in there."
- Don't recompute discount logic anywhere other than `OrderService.CalculateTotal`.
