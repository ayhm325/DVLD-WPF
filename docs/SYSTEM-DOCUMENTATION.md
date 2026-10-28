# DVLD — Complete System Documentation

## 1. Executive summary

DVLD (Driving & Vehicle License Department) is a .NET 8 application for managing people, users, drivers, applications, tests, licenses, detention/release, and international licensing workflows, with a WPF desktop client (`Presentation (WPF)`) and a browser-based React client (`ClientApp`).

The solution separates:

- Domain concepts
- Application use cases and business workflows
- Infrastructure/database access
- API transport contracts
- ASP.NET Core HTTP/security concerns
- WPF desktop presentation (`Presentation (WPF)`)
- React browser frontend (`ClientApp`, documented separately under [`ClientApp/README.md`](../ClientApp/README.md) and [`ClientApp/docs/`](../ClientApp/docs/ARCHITECTURE.md))
- Automated tests

## 2. Solution structure

```text
DVLD-WPF/
├── Domain/
├── Application/
├── Infrastructure/
├── DVLD.Contracts/
├── API/
│   └── DVLD.Api/
├── Presentation (WPF)/
│   └── DVLD_WPF.sln
├── ClientApp/
│   ├── README.md
│   └── docs/
├── Test/
└── docs/
```

## 3. Runtime architecture

```text
Presentation (WPF)     ClientApp (React)
        │                      │
        └───┬──────────────────┘
            ↓ HTTP + JSON + JWT
         DVLD.Api (CORS: "ReactApp" for http://localhost:5173)
            ↓
        Application
            ↓
      Infrastructure
            ↓
        SQL Server
```

The `Presentation (WPF)` project shares `DVLD.Contracts` with the API but does not reference the API project directly. `ClientApp` is a standalone React/TypeScript/Vite frontend that communicates with `DVLD.Api` over HTTP + JSON + JWT.

## 4. Technology stack

| Area | Technology |
|---|---|
| Language | C# (.NET) / TypeScript (React frontend) |
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| Desktop UI | WPF (`Presentation (WPF)`) |
| Web UI | React 19 + TypeScript + Vite (`ClientApp`) |
| UI pattern | MVVM-oriented (WPF) / Component SPA (React) |
| MVVM toolkit | CommunityToolkit.Mvvm |
| Authentication | JWT bearer |
| Authorization | ASP.NET Core policies (`AdminOnly`, `StaffOnly`, `StaffOrAdmin`) |
| Password hashing | BCrypt (`BCrypt.Net-Next`) |
| Persistence | Repository + Unit of Work |
| API contracts | DVLD.Contracts |
| HTTP client | HttpClientFactory (WPF) / Fetch API (React) |
| Testing | Unit + integration tests (.NET) |
| CI | Azure DevOps Pipelines |

## 5. Functional scope

The system manages:

- People
- Users
- Drivers
- Countries
- Application types
- License classes
- Applications
- Local driving-license applications
- Test types
- Test appointments
- Test results
- Licenses
- Detained licenses
- International licenses
- Dashboard statistics

Major workflows:

- Local license application
- Test scheduling
- Test result recording
- First-license issuance
- Renewal
- Replacement
- Detention
- Release
- International-license issuance

## 6. Layer responsibilities

### Domain

Core entities, enums, and domain concepts.

### Application

Use cases, business workflows, validation, DTOs, Result types, repository abstractions, Unit of Work abstractions, and current-user abstraction.

### Infrastructure

EF Core DbContext/configuration, repositories, Unit of Work implementation, transactions, and SQL Server persistence.

### DVLD.Contracts

HTTP request/response models shared between API and WPF client.

### API

HTTP routing, model binding, authentication/authorization integration, contract mapping, Result-to-HTTP mapping, and global exception handling.

### Presentation (WPF)

WPF Views, ViewModels, Pages, Windows, UserControls, commands, API clients, session state, navigation, and notifications.

### ClientApp (React)

Standalone React + TypeScript + Vite single-page application consuming `DVLD.Api` at the HTTP/JSON/JWT boundary. Because the React frontend is under active iterative development, its internal architecture, API integration, feature implementation status, and developer workflow are documented separately in [`ClientApp/README.md`](../ClientApp/README.md) and [`ClientApp/docs/`](../ClientApp/docs/ARCHITECTURE.md).

## 7. Dependency Injection

### API

`API/DVLD.Api/Program.cs` is the composition root and registers:

- `DVLDDbContext` (with `EnableRetryOnFailure()`)
- `UnitOfWork`
- repositories (15 scoped repositories)
- Application services (21 scoped services)
- current-user implementation (`ApiCurrentUserService`)
- authentication/authorization and active-user validation
- CORS (`"ReactApp"` for `http://localhost:5173`)
- rate limiting (`LoginRateLimit`)
- exception handling (`GlobalExceptionHandler` in `API/DVLD.Api/Security/GlobalExceptionHandler.cs`)
- controllers/Swagger

Runtime DbContext, Unit of Work, repositories, and Application services are scoped.

### WPF

`Presentation (WPF)/App.xaml.cs` is the Presentation composition root.

It registers:

- current-user session
- API host service
- window/navigation services
- notification services
- HttpClientFactory API clients
- feature API clients
- ViewModels
- Windows
- Pages

## 8. WPF/API interaction

```text
View
 ↓
ViewModel
 ↓
Feature API Client
 ↓
IApiClient
 ↓
HttpClient
 ↓
HTTP API
```

`ApiClient` centralizes bearer-token handling, JSON requests, common HTTP errors, network failures, and timeout handling.

The access token is kept in the in-memory current-user session.

Remember Me persists username/preferences, not a persistent authentication token.

## 9. Presentation architecture qualification

The WPF layer uses MVVM as its primary pattern.

It is not documented as pure MVVM because some view-specific navigation, window behavior, animations, and UI interactions remain in code-behind.

Role-based visibility is a usability feature. API authorization is the security boundary.

## 10. Database model

Current entities/tables (14):

- Applications
- ApplicationTypes
- Countries
- DetainedLicenses
- Drivers
- InternationalLicenses
- Licenses
- LicenseClasses
- LocalDrivingLicenseApplications
- People
- Tests
- TestAppointments
- TestTypes
- Users

Data access (no SQL views are defined or queried): `DVLDDbContext` maps these 14 tables through EF Core configurations. Repositories use LINQ queries, plus raw SQL only for `UPDLOCK` row-lock reads on base tables and one dashboard aggregate query (`SqlQueryRaw`). See `DATABASE.md` for details.

Important integrity rules include:

- unique NationalNo
- unique UserName
- unique User.PersonId
- unique Driver.PersonId
- unique LocalDrivingLicenseApplication.ApplicationID
- one Test per TestAppointment
- one active license per Driver + LicenseClass
- one active InternationalLicense per Driver
- one unreleased detention per License

## 11. Result architecture

Application operations return `Result` or `Result<T>`.

Known categories include:

- Validation
- NotFound
- Conflict
- Forbidden
- Failure

The API maps these outcomes to HTTP responses through a shared mapper (`ResultHttpMapper`).

## 12. Unit of Work and transactions

The runtime uses one scoped `DVLDDbContext` per request scope.

`UnitOfWork` (`Infrastructure/UnitOfWork.cs`) coordinates repository operations against that shared persistence context.

Critical multi-write workflows use explicit transactions with `IsolationLevel.Serializable`:

- `EnsureNoActiveTransaction()` guards against nested transactions on the same `UnitOfWork`.
- Inside `ExecuteInTransactionAsync`, calling `transaction.CommitAsync()` flushes pending tracked changes (`SaveChangesAsync`) and defers the physical SQL commit until the workflow delegate returns.
- If the delegate returns a failed `Result` (`Result { IsFailure: true }`) or throws an exception, `UnitOfWork` rolls back the transaction and clears `DVLDDbContext.ChangeTracker`.
- When a transient error occurs during physical commit under `EnableRetryOnFailure()`, `VerifyCommitOutcomeAsync` checks an optional `verifySucceeded` predicate or `TransactionVerificationTracker` (`SavingChanges`/`SavedChanges` probes) to confirm whether SQL Server already committed the transaction before allowing a retry, preventing duplicate execution of committed operations. Verification happens only when a `verifySucceeded` callback was supplied or the tracker has recorded probes (`tracker.CanVerify`); otherwise `UnitOfWork` throws an `InvalidOperationException` rather than retrying.

## 13. Business workflow summary

### Local license

```text
Person
 ↓
Application
 ↓
LocalDrivingLicenseApplication
 ↓
Theory
 ↓
Written
 ↓
Practical
 ↓
All required tests passed
 ↓
First license
```

### Test result

Authenticated user → Serializable transaction → protected appointment read → validation → Test creation → appointment lock → SaveChanges → Commit.

### First license

New local application → all required tests passed → driver resolution → no conflicting active license → create license → complete application → commit.

### Renewal

Expired active license → renewal application → deactivate old license → create new license → complete application → commit.

### Replacement

Active/non-expired license + Lost/Damaged reason → replacement application → deactivate old license → create replacement with old expiration → commit.

### Detention/release

Detain eligible license → create detention → deactivate license.

Release detention → create release application → mark detention released → reactivate only when conditions allow → commit.

### International

Class 3 active/non-expired local license → validation → application type 6 → one-year international license → commit.

## 14. API

The API currently contains 20 controllers and **101 HTTP actions**.

See `API.md` for the complete endpoint inventory.

## 15. Security

Implemented security controls include:

- JWT bearer authentication
- issuer/audience/signing-key/lifetime validation
- zero clock skew
- active-user validation (`OnTokenValidated` via `IUserService.IsUserActiveAsync`)
- policy-based authorization (`AdminOnly`, `StaffOnly` for `Staff` role only, `StaffOrAdmin`)
- CORS policy `"ReactApp"` (`http://localhost:5173`)
- BCrypt password hashing
- login rate limiting (`LoginRateLimit`)
- current-user abstraction (`ICurrentUserService`)
- global exception handling (`GlobalExceptionHandler` in `API/DVLD.Api/Security/GlobalExceptionHandler.cs`, returning generic HTTP 500 `ProblemDetails` for unexpected errors and HTTP `499 Client Closed Request` when `exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested`)
- database integrity constraints
- transactional/concurrency protection

## 16. Testing

Three test projects exist:

- `Application.UnitTests`
- `API.IntegrationTests`
- `Infrastructure.IntegrationTests`

API integration testing distinguishes controller boundary tests (including `GlobalExceptionHandlerTests` and real `JwtBearer` token/active-user tests in `AuthControllerTests`) from real workflow integration tests.

Workflow tests exercise:

```text
HTTP
 → Controller
 → real Application
 → real Infrastructure
 → EF Core
 → isolated SQL Server
```

## 17. Test inventory and verification status

### Current source & VSTest inventory (commit `4f75658`)

| Suite | Source Methods (`[Fact]+[Theory]`) | Source Test Cases (`[Fact]+[InlineData]`) | VSTest Discovered (`--list-tests`) | Audit Execution Outcome |
|---|---:|---:|---:|---|
| `Application.UnitTests` | 603 | 642 | 633 | Executed (`--no-build`): **633 Passed, 0 Failed, 0 Skipped** |
| `API.IntegrationTests` | 362 | 442 | 435 | Discovered (`435` entries); not executed against live SQL Server during audit |
| `Infrastructure.IntegrationTests` | 469 | 507 | 494 | Discovered (`494` entries); not executed against live SQL Server during audit |
| **Total** | **1,434** | **1,591** | **1,562** | `Application.UnitTests` passed; integration suites enumerated only |

Discovered VSTest counts (`435` and `494`) reflect test discovery only and are not claimed as passing test executions. During the audit inside a network-isolated sandbox, `dotnet build` failed (`NU1900` / sandbox MSBuild restriction), so `--no-build` execution/discovery used existing `bin/Debug/net8.0` assemblies. Those assemblies were built on 2026-10-26 (12:39–15:21), before commits `b5dc945`, `3056f88`, and `61cbdcc` added 29 source test cases, which is why VSTest counts (`1,562`) are lower than source attribute counts at `HEAD` (`1,591`). Source counts are static attribute counts, not executed test results; only the `633` `Application.UnitTests` entries were actually executed.

### Historical full test run result

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Application.UnitTests | 631 | 0 | 0 |
| API.IntegrationTests | 364 | 0 | 0 |
| Infrastructure.IntegrationTests | 492 | 0 | 0 |
| **Total** | **1487** | **0** | **0** |

These are historical user-reported results from earlier development, not a fresh full-suite execution claim.

## 18. CI

Azure DevOps (`azure-pipelines.yml`) currently provides Continuous Integration for the `.NET` solution (`"Presentation (WPF)/DVLD_WPF.sln"`):

```text
main
 ↓
.NET 8
 ↓
restore
 ↓
Release build
 ↓
tests
 ↓
publish test results
```

`azure-pipelines.yml` does not currently build or lint `ClientApp/`, and no production deployment/CD stage is claimed.

## 19. Database migrations

```text
20260901000000_InitialCreate
20260903234022_AddUniqueLocalApplicationApplicationId
20260906111058_AddLicenseInternationalAndDetainedConstraints
20260907233340_AddUserRole
```

See `DATABASE.md` for schema and data-access details.

## 20. Design patterns

Implemented patterns include:

- Dependency Injection
- Repository
- Unit of Work
- Service Layer
- DTO
- Result Pattern
- MVVM-oriented presentation

This is not presented as a textbook implementation of one named architecture framework.

## 21. Future improvements

These are future improvements, not current implementation claims:

- API versioning
- health checks
- production secret-vault integration
- security headers
- structured/correlation logging enhancements
- dependency vulnerability scanning
- dedicated architecture tests
- performance/load testing
- additional API abuse/security tests

## 22. Documentation map

| Document | Purpose |
|---|---|
| `docs/SYSTEM-DOCUMENTATION.md` | System-wide overview |
| `docs/ARCHITECTURE.md` | Architectural boundaries |
| `docs/API.md` | HTTP endpoint reference |
| `docs/BUSINESS-WORKFLOWS.md` | Business rules and workflows |
| `docs/DATABASE.md` | SQL/EF model, data access, and constraints |
| `docs/SECURITY.md` | Security controls |
| `docs/TESTING.md` | Test architecture and verification |
| `docs/DOCUMENTATION-AUDIT.md` | Source-based documentation audit |
| [`ClientApp/README.md`](../ClientApp/README.md) | React frontend setup and operational guide |
| [`ClientApp/docs/`](../ClientApp/docs/ARCHITECTURE.md) | Detailed React frontend architecture, API integration, feature status, and development docs |
