# DVLD — Complete System Documentation

## 1. Executive summary

DVLD (Driving & Vehicle License Department) is a .NET 8 application for managing people, users, drivers, applications, tests, licenses, detention/release, and international licensing workflows.

The solution separates:

- Domain concepts
- Application use cases and business workflows
- Infrastructure/database access
- API transport contracts
- ASP.NET Core HTTP/security concerns
- WPF presentation
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
├── Presentation/
├── Test/
└── docs/
```

## 3. Runtime architecture

```text
WPF
 ↓ HTTP + JSON + JWT
DVLD.Api
 ↓
Application
 ↓
Infrastructure
 ↓
SQL Server
```

The WPF project shares `DVLD.Contracts` with the API but does not reference the API project directly.

## 4. Technology stack

| Area | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Database | SQL Server |
| Desktop UI | WPF |
| UI pattern | MVVM-oriented |
| MVVM toolkit | CommunityToolkit.Mvvm |
| Authentication | JWT bearer |
| Authorization | ASP.NET Core policies |
| Password hashing | BCrypt |
| Persistence | Repository + Unit of Work |
| API contracts | DVLD.Contracts |
| HTTP client | HttpClientFactory |
| Testing | Unit + integration tests |
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

### Presentation

WPF Views, ViewModels, Pages, Windows, UserControls, commands, API clients, session state, navigation, and notifications.

## 7. Dependency Injection

### API

`Program.cs` is the composition root and registers:

- DbContext
- Unit of Work
- repositories
- Application services
- current-user implementation
- authentication/authorization
- rate limiting
- exception handling
- controllers/Swagger

Runtime DbContext, Unit of Work, repositories, and Application services are scoped.

### WPF

`App.xaml.cs` is the Presentation composition root.

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

Current entities/tables:

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

The API maps these outcomes to HTTP responses through a shared mapper.

## 12. Unit of Work and transactions

The runtime uses one scoped DbContext per request scope.

The Unit of Work coordinates repository operations against that shared persistence context.

Critical multi-write workflows use explicit transactions.

Serializable isolation is used for concurrency-sensitive operations.

The transaction helper uses EF Core execution-strategy support and rolls back/clears tracked state on failure.

The workflow callback remains responsible for SaveChanges and Commit.

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
- active-user validation
- policy-based authorization
- BCrypt password hashing
- login rate limiting
- current-user abstraction
- global exception handling
- generic ProblemDetails
- database integrity constraints
- transactional/concurrency protection

## 16. Testing

Three test projects exist:

- Application.UnitTests
- API.IntegrationTests
- Infrastructure.IntegrationTests

API integration testing distinguishes controller boundary tests from real workflow integration tests.

Workflow tests exercise:

```text
HTTP
 → Controller
 → real Application
 → real Infrastructure
 → EF Core
 → isolated SQL Server
```

## 17. Historical test result

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Application.UnitTests | 631 | 0 | 0 |
| API.IntegrationTests | 364 | 0 | 0 |
| Infrastructure.IntegrationTests | 492 | 0 | 0 |
| **Total** | **1487** | **0** | **0** |

These are historical user-reported results, not a fresh execution claim.

## 18. CI

Azure DevOps currently provides Continuous Integration:

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

No production deployment/CD stage is claimed.

## 19. Database migrations

```text
20260901000000_InitialCreate
20260903234022_AddUniqueLocalApplicationApplicationId
20260906111058_AddLicenseInternationalAndDetainedConstraints
20260907233340_AddUserRole
```

See `DATABASE.md` for schema details.

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
| SYSTEM-DOCUMENTATION.md | System-wide overview |
| ARCHITECTURE.md | Architectural boundaries |
| API.md | HTTP endpoint reference |
| BUSINESS-WORKFLOWS.md | Business rules and workflows |
| DATABASE.md | SQL/EF model and constraints |
| SECURITY.md | Security controls |
| TESTING.md | Test architecture and verification |
