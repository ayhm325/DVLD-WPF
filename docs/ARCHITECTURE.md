# DVLD Architecture Guide

## 1. Overview

DVLD is a layered .NET 8 system with desktop (WPF) and browser (React) clients communicating with an ASP.NET Core Web API. Its major boundaries are:

- **Domain** (`Domain/`) — core entities, enums, and domain concepts.
- **Application** (`Application/`) — use cases, business workflows, validation, DTOs, Result types, and abstractions.
- **Infrastructure** (`Infrastructure/`) — EF Core, SQL Server, repositories, Unit of Work, and transactions.
- **DVLD.Contracts** (`DVLD.Contracts/`) — transport-facing request/response contracts shared by the API and the .NET desktop client.
- **DVLD.Api** (`API/DVLD.Api/`) — HTTP boundary, authentication/authorization, CORS, mapping, and API error handling.
- **Presentation (WPF)** (`Presentation (WPF)/`) — WPF desktop UI, ViewModels, API clients, navigation, notifications, and presentation state.
- **ClientApp (React)** (`ClientApp/`) — standalone React + TypeScript + Vite single-page application consuming the HTTP API. Detailed frontend documentation is maintained separately under [`ClientApp/README.md`](../ClientApp/README.md) and [`ClientApp/docs/`](../ClientApp/docs/ARCHITECTURE.md).

## 2. Compile-time dependencies

```text
Application ───────────────→ Domain

Infrastructure ────────────→ Application
Infrastructure ────────────→ Domain

DVLD.Api ──────────────────→ Application
DVLD.Api ──────────────────→ Infrastructure
DVLD.Api ──────────────────→ DVLD.Contracts

Presentation (WPF) ────────→ DVLD.Contracts
```

The `Presentation (WPF)` project (`Presentation.csproj` inside `Presentation (WPF)/DVLD_WPF.sln`) does not reference `DVLD.Api`, `Application`, `Infrastructure`, or `Domain` directly. `ClientApp` is an independent Node.js/Vite project (`ClientApp/package.json`) outside the .NET solution and has no compile-time reference to any .NET project.

## 3. Runtime communication

```text
WPF Views / Pages / Windows          React SPA (ClientApp)
            ↓                                 ↓
        ViewModels                   Frontend API Services
            ↓                                 ↓
    Feature API Clients                  fetch (CORS)
            ↓                                 ↓
        IApiClient                            │
            ↓                                 │
        HttpClient                            │
            └───────────────┬─────────────────┘
                            ↓
                     HTTP + JSON + JWT
                            ↓
                        DVLD.Api
                            ↓
                   Application Services
                            ↓
                 Repository / Unit of Work
                            ↓
                         EF Core
                            ↓
                       SQL Server
```

Compile-time references and runtime communication are intentionally different concepts. Both clients communicate with `DVLD.Api` at runtime over HTTP + JSON + JWT (`http://localhost:5260`). Browser requests from `ClientApp` (`http://localhost:5173`) are permitted at the API boundary through the `"ReactApp"` CORS policy.

## 4. Responsibility matrix

| Project / Directory | Primary responsibility |
|---|---|
| `Domain` | Core model and domain concepts |
| `Application` | Use cases, workflows, validation, DTOs, abstractions |
| `Infrastructure` | Persistence, EF Core, repositories, Unit of Work, transactions |
| `DVLD.Contracts` | Shared .NET HTTP request/response contracts |
| `API/DVLD.Api` | HTTP, authentication, authorization, CORS, mapping, API error boundary |
| `Presentation (WPF)` | WPF desktop UI, ViewModels, commands, API clients, presentation state |
| `ClientApp` | React browser UI and HTTP integration (documented in [`ClientApp/docs/`](../ClientApp/docs/ARCHITECTURE.md)) |

## 5. Composition roots

### API

`API/DVLD.Api/Program.cs` is the API composition root. It configures:

- `DVLDDbContext` (with SQL Server `EnableRetryOnFailure()`)
- `UnitOfWork`
- repositories (15 scoped repositories)
- Application services (21 scoped services)
- current-user implementation (`ApiCurrentUserService`)
- JWT authentication and active-user token validation
- authorization policies (`AdminOnly`, `StaffOnly`, `StaffOrAdmin`)
- CORS policy (`"ReactApp"` for `http://localhost:5173`)
- rate limiting (`LoginRateLimit`)
- global exception handling (`GlobalExceptionHandler` in `API/DVLD.Api/Security/GlobalExceptionHandler.cs`)
- `ProblemDetails`
- controllers
- Swagger

The runtime `DVLDDbContext` is scoped.

### WPF

`Presentation (WPF)/App.xaml.cs` is the WPF Presentation composition root. It configures:

- current-user session
- API host service
- window/navigation services
- notification services
- HttpClientFactory clients
- feature API clients
- ViewModels
- Windows
- Pages

The WPF service provider uses build/scope validation.

## 6. Dependency Injection

Dependency Injection supplies abstractions to consumers instead of forcing consumers to construct concrete dependencies.

The project uses DI for:

- Application services
- repositories
- Unit of Work
- current-user access
- API clients
- notifications
- WPF presentation services

This supports separation of responsibilities and makes Application services easier to unit-test.

## 7. Repository and Unit of Work

The runtime API uses one scoped `DVLDDbContext` per request scope.

Repositories operate against that shared persistence context. `IUnitOfWork` coordinates:

- `SaveChanges`
- transaction creation
- transaction execution using EF Core execution strategy

This is important for multi-write workflows. Independent DbContexts and independent commits would weaken atomicity; the current runtime design coordinates related changes through one Unit of Work.

## 8. Transaction model

Critical workflows follow the general pattern:

```text
Begin transaction (EnsureNoActiveTransaction)
      ↓
Read protected state
      ↓
Validate business invariants
      ↓
Create/update related records
      ↓
SaveChanges
      ↓
Request Commit (flushes pending changes; defers physical commit)
      ↓
Operation returns Result
      ↓
Physical Commit (or Rollback + ChangeTracker.Clear on failure Result)
```

`UnitOfWork` (`Infrastructure/UnitOfWork.cs`) implements the following transactional guarantees:

- **Nested-transaction guard:** `EnsureNoActiveTransaction()` rejects nested transactions on the same `UnitOfWork` instance (`InvalidOperationException`).
- **Deferred physical commit inside `ExecuteInTransactionAsync`:** When a workflow runs inside `ExecuteInTransactionAsync`, calling `transaction.CommitAsync()` flushes any remaining tracked changes (`SaveChangesAsync`) and records the commit request, while deferring the physical `IDbContextTransaction.CommitAsync` until the delegate returns.
- **Automatic rollback on failure `Result` or exception:** If the delegate returns a failed `Result` (`Result { IsFailure: true }`) or throws an exception, `UnitOfWork` rolls back the database transaction and clears `DVLDDbContext.ChangeTracker` so stale tracked entities cannot leak into subsequent operations or retry attempts.
- **Ambiguous-commit verification across retries:** Because `DVLDDbContext` uses SQL Server retry execution strategy (`EnableRetryOnFailure()`), a transient failure during the physical commit step could leave the application uncertain whether SQL Server committed the transaction before the connection dropped. `UnitOfWork` resolves this via `VerifyCommitOutcomeAsync`:
  - Callers can supply an explicit `verifySucceeded` predicate (`IUnitOfWork.ExecuteInTransactionAsync` overload), or rely on `TransactionVerificationTracker`, which captures `SavingChanges`/`SavedChanges` probes for inserted identity keys and modified scalar properties.
  - If verification confirms the commit succeeded on SQL Server, `UnitOfWork` clears the change tracker and returns the operation's result without re-executing the business delegate.
  - If verification confirms the transaction was not committed, the transient exception is rethrown so the EF Core execution strategy can retry cleanly.
- **Serializable isolation:** Used for concurrency-sensitive workflows.

## 9. Client boundaries (WPF and React)

### WPF architecture (`Presentation (WPF)`)

The WPF Presentation layer is **MVVM-oriented**, not described as pure MVVM.

It uses:

- `CommunityToolkit.Mvvm` (`ObservableObject`, `ObservableProperty`, `RelayCommand`)
- ViewModels, Views, Pages, Windows, and UserControls

Some view-specific navigation, window management, animations, and UI interaction remain in code-behind.

```text
ViewModel
   ↓
Feature API Client
   ↓
IApiClient
   ↓
HttpClientFactory
   ↓
HTTP API
```

### React architecture (`ClientApp`)

`ClientApp` is a standalone React 19 + TypeScript + Vite single-page application that communicates with `DVLD.Api` (`http://localhost:5260/api`) using browser `fetch` and Bearer JWT authentication, authorized for cross-origin requests from `http://localhost:5173` via the `"ReactApp"` CORS policy.

To keep core backend and WPF documentation stable while the React frontend evolves, internal React architecture, API integration details, feature implementation status, and frontend development instructions are documented under [`ClientApp/README.md`](../ClientApp/README.md) and [`ClientApp/docs/`](../ClientApp/docs/ARCHITECTURE.md).

## 10. Local API hosting

`ApiHostService` (`Presentation (WPF)/Services/ApiHostService.cs`) checks the local API endpoint (`http://localhost:5260`) and can start the API process when necessary, then waits for it to become available.

This is a local execution convenience for the WPF desktop client. It is not documented as a production deployment mechanism.

## 11. Security boundary

Clients (WPF and React) may hide or disable UI according to role and workflow state.

That is not the security boundary.

`DVLD.Api` enforces authentication, active-user validation, CORS, rate limiting, and authorization policies independently.

## 12. Contracts vs Application DTOs

`DVLD.Contracts` contains transport-facing request/response models shared by `DVLD.Api` and `Presentation (WPF)`.

Application DTOs remain internal to the Application layer.

Controllers map between transport contracts and Application DTOs, preventing API transport types from becoming persistence entities.

## 13. Design-time EF Core

Runtime database access uses the DI-registered scoped `DVLDDbContext`.

`DVLDDbContextFactory` (`Infrastructure/DVLDDbContextFactory.cs`) implements `IDesignTimeDbContextFactory<DVLDDbContext>` for EF Core design-time operations such as migrations. It is not the runtime persistence path.

## 14. Architectural principles

- Keep controllers thin.
- Keep business workflows in Application.
- Keep EF Core and SQL concerns in Infrastructure.
- Use abstractions at architectural boundaries.
- Define transaction boundaries around multi-write workflows.
- Use database constraints as an additional integrity layer.
- Keep UI usability rules separate from backend authorization.
- Keep documentation synchronized with implementation.
