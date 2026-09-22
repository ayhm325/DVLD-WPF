# DVLD Architecture Guide

## 1. Overview

DVLD is a layered .NET 8 system. Its major boundaries are:

- **Domain** — core entities, enums, and domain concepts.
- **Application** — use cases, business workflows, validation, DTOs, Result types, and abstractions.
- **Infrastructure** — EF Core, SQL Server, repositories, Unit of Work, and transactions.
- **DVLD.Contracts** — transport-facing request/response contracts.
- **DVLD.Api** — HTTP boundary, authentication/authorization, mapping, and API error handling.
- **Presentation** — WPF UI, ViewModels, API clients, navigation, notifications, and presentation state.

## 2. Compile-time dependencies

```text
Application ───────────────→ Domain

Infrastructure ────────────→ Application
Infrastructure ────────────→ Domain

DVLD.Api ──────────────────→ Application
DVLD.Api ──────────────────→ Infrastructure
DVLD.Api ──────────────────→ DVLD.Contracts

Presentation ──────────────→ DVLD.Contracts
```

The Presentation project does not reference `DVLD.Api` directly.

## 3. Runtime communication

```text
WPF Views / Pages / Windows
            ↓
        ViewModels
            ↓
    Feature API Clients
            ↓
        IApiClient
            ↓
       HttpClient
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

Compile-time references and runtime communication are intentionally different concepts.

## 4. Responsibility matrix

| Project | Primary responsibility |
|---|---|
| Domain | Core model and domain concepts |
| Application | Use cases, workflows, validation, DTOs, abstractions |
| Infrastructure | Persistence, EF Core, repositories, Unit of Work, transactions |
| DVLD.Contracts | HTTP request/response contracts |
| DVLD.Api | HTTP, authentication, authorization, mapping, API error boundary |
| Presentation | WPF UI, ViewModels, commands, API clients, presentation state |

## 5. Composition roots

### API

`Program.cs` is the API composition root. It configures:

- DbContext
- Unit of Work
- repositories
- Application services
- current-user implementation
- JWT authentication
- authorization policies
- rate limiting
- global exception handling
- ProblemDetails
- controllers
- Swagger

The runtime `DVLDDbContext` is scoped.

### WPF

`App.xaml.cs` is the Presentation composition root. It configures:

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
Begin transaction
      ↓
Read protected state
      ↓
Validate business invariants
      ↓
Create/update related records
      ↓
SaveChanges
      ↓
Commit
```

`ExecuteInTransactionAsync` uses the EF Core execution strategy. On failure it rolls back and clears tracked state before rethrowing.

The workflow callback is responsible for its SaveChanges and Commit sequence.

Serializable isolation is used for concurrency-sensitive workflows.

## 9. WPF architecture

The Presentation layer is **MVVM-oriented**, not described as pure MVVM.

It uses:

- CommunityToolkit.Mvvm
- `ObservableObject`
- `ObservableProperty`
- `RelayCommand`
- ViewModels
- Views
- Pages
- Windows
- UserControls

Some view-specific navigation, window management, animations, and UI interaction remain in code-behind.

### API-client path

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

## 10. Local API hosting

`ApiHostService` checks the local API endpoint and can start the API process when necessary, then waits for it to become available.

This is a local execution convenience. It is not documented as a production deployment mechanism.

## 11. Security boundary

WPF may hide or disable UI according to role and workflow state.

That is not the security boundary.

The API enforces authentication and authorization policies independently.

## 12. Contracts vs Application DTOs

`DVLD.Contracts` contains transport-facing request/response models shared by API and Presentation.

Application DTOs remain internal to the Application layer.

Controllers map between transport contracts and Application DTOs, preventing API transport types from becoming persistence entities.

## 13. Design-time EF Core

Runtime database access uses the DI-registered scoped DbContext.

`DVLDDbContextFactory` implements `IDesignTimeDbContextFactory<DVLDDbContext>` for EF Core design-time operations such as migrations. It is not the runtime persistence path.

## 14. Architectural principles

- Keep controllers thin.
- Keep business workflows in Application.
- Keep EF Core and SQL concerns in Infrastructure.
- Use abstractions at architectural boundaries.
- Define transaction boundaries around multi-write workflows.
- Use database constraints as an additional integrity layer.
- Keep UI usability rules separate from backend authorization.
- Keep documentation synchronized with implementation.
