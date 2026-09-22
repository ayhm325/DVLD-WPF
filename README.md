# DVLD — Driving & Vehicle License Department System

DVLD is a .NET 8 driving-license department management system with a WPF desktop client, ASP.NET Core Web API, SQL Server persistence, transactional business workflows, authentication/authorization, and automated testing.

## Functional scope

The system manages:

- People and users
- Drivers
- Countries
- Applications and application types
- License classes
- Local driving-license applications
- Test types, appointments, and results
- First-license issuance
- License renewal and replacement
- License detention and release
- International licenses
- Dashboard statistics

## Architecture

```text
WPF Presentation
       |
       | HTTP + JSON + JWT
       v
DVLD.Api
       |
       v
Application
       |
       v
Infrastructure
       |
       v
SQL Server
```

`DVLD.Contracts` is the shared HTTP contract boundary between the API and WPF client. The WPF project does not reference the API project directly.

## Solution structure

```text
Domain/
Application/
Infrastructure/
DVLD.Contracts/
API/DVLD.Api/
Presentation/
Test/
docs/
```

## Technology stack

- C# / .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server
- WPF
- CommunityToolkit.Mvvm
- JWT bearer authentication
- ASP.NET Core policy-based authorization
- BCrypt password hashing
- Dependency Injection
- Repository + Unit of Work
- Result / Result<T> pattern
- DTOs and API contracts
- HttpClientFactory
- Unit and integration testing
- Azure DevOps Pipelines

## Architectural characteristics

- Thin API controllers
- Application services own use-case orchestration
- Infrastructure owns EF Core and SQL Server persistence
- Shared scoped DbContext / Unit of Work for transactional workflows
- Serializable transactions for concurrency-sensitive workflows
- Database constraints reinforce business invariants
- Backend authorization is the security boundary
- WPF role-based UI is a usability mechanism, not the security boundary
- Presentation is MVVM-oriented, not claimed to be pure MVVM

## Run the API

```powershell
dotnet restore
dotnet build --configuration Release
dotnet run --project ".\API\DVLD.Api\DVLD.Api.csproj" --launch-profile http
```

The local HTTP profile has been used at `http://localhost:5260`; current launch settings are authoritative if the port changes.

## Run all tests

```powershell
dotnet test --configuration Release
```

Individual suites:

```powershell
dotnet test ".\Test\Application.UnitTests\Application.UnitTests.csproj" --configuration Release
dotnet test ".\Test\API.IntegrationTests\API.IntegrationTests.csproj" --configuration Release
dotnet test ".\Test\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj" --configuration Release
```

## Documentation

- [System Documentation](docs/SYSTEM-DOCUMENTATION.md)
- [Architecture](docs/ARCHITECTURE.md)
- [API Reference](docs/API.md)
- [Business Workflows](docs/BUSINESS-WORKFLOWS.md)
- [Database](docs/DATABASE.md)
- [Security](docs/SECURITY.md)
- [Testing](docs/TESTING.md)

## Historical test result

The latest complete result reported during development was:

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Application.UnitTests | 631 | 0 | 0 |
| API.IntegrationTests | 364 | 0 | 0 |
| Infrastructure.IntegrationTests | 492 | 0 | 0 |
| **Total** | **1487** | **0** | **0** |

These are historical user-reported results, not a claim that the documentation update freshly executed all 1487 tests.

## Continuous Integration

The Azure DevOps pipeline currently provides Continuous Integration:

```text
main push
    ↓
.NET 8 SDK
    ↓
Restore
    ↓
Release Build
    ↓
Tests
    ↓
Publish Test Results
```

No production deployment/CD stage is claimed by the current documentation.

## Documentation principle

Implemented behavior, verified test behavior, and future improvements are kept distinct. When source behavior changes, update the relevant documentation in the same change.
