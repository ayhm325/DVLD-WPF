# DVLD Testing Guide

## 1. Test architecture

The repository contains three test projects:

```text
Test/
├── Application.UnitTests/
├── API.IntegrationTests/
└── Infrastructure.IntegrationTests/
```

They test different architectural boundaries.

## 2. Application unit tests

`Application.UnitTests` contains 21 service-oriented test classes covering areas including:

- Dashboard
- Countries
- Authentication
- Licenses
- License queries
- License classes
- Tests
- Test types
- Users
- People
- Drivers
- Application types
- Applications
- Test workflow
- Test appointments
- Local driving-license applications
- License issuance
- License renewal
- License replacement
- Detained licenses
- International licenses

These tests focus on Application-layer behavior without requiring the full HTTP/database stack.

## 3. API integration tests

`API.IntegrationTests` contains controller/API boundary tests and workflow integration tests.

### Controller/API boundary tests

These use `WebApplicationFactory<Program>` (`ApiWebApplicationFactory`) with Application services mocked where appropriate, covering all 20 controller suites plus `GlobalExceptionHandlerTests` (`Test/API.IntegrationTests/Middleware/GlobalExceptionHandlerTests.cs`).

They verify:

- routing
- HTTP methods
- authorization behavior
- request binding
- response mapping
- status codes
- ProblemDetails and client-cancellation (HTTP 499 in `GlobalExceptionHandler`) mapping
- controller/Application interaction

They should not be described as full-stack tests.

### Workflow integration tests

Workflow tests use the real Application and Infrastructure stack with an isolated SQL Server database.

```text
HTTP
 ↓
Controller
 ↓
Real Application
 ↓
Real Unit of Work / Repository
 ↓
EF Core
 ↓
Isolated SQL Server
```

Authentication is replaced by `TestAuthenticationHandler` so tests can deterministically construct the required claims.

## 4. Workflow test classes

Current workflow coverage includes:

- `TestResultWorkflowTests`
- `LicenseRenewalWorkflowTests`
- `DetainedLicenseWorkflowTests`
- `LicenseIssuanceWorkflowTests`
- `LicenseReplacementWorkflowTests`
- `TestAppointmentWorkflowTests`
- `InternationalLicenseWorkflowTests`
- `ReleaseDetainedLicenseWorkflowTests`
- `LocalDrivingLicenseApplicationWorkflowTests`

## 5. Infrastructure integration tests

`Infrastructure.IntegrationTests` contains repository test classes plus:

- `UnitOfWorkTests`
- `SqlServerTestDatabaseTests`

The suite verifies real persistence behavior including:

- EF Core mappings
- CRUD
- foreign keys
- unique constraints
- filtered unique indexes
- transactions
- rollback
- database connectivity
- migration state
- concurrency behavior

## 6. SQL Server test isolation

The integration-test fixture obtains its base connection through:

```text
DVLD_TEST_CONNECTION
```

Test databases are isolated by fixture/factory and removed afterward.

Migrations are applied.

Tests verify database reachability and that migrations are not unexpectedly pending.

This is a real SQL Server integration strategy rather than an in-memory EF provider.

## 7. Unit of Work tests

`UnitOfWorkTests` (`Test/Infrastructure.IntegrationTests/UnitOfWorkTests.cs`, 11 test methods) covers transaction-related behavior including:

- `SaveChanges` and explicit transaction commit
- rollback and `ChangeTracker.Clear()` verification on explicit rollback or uncommitted disposal
- automatic rollback and `ChangeTracker.Clear()` when an operation inside `ExecuteInTransactionAsync` returns a failure `Result` or throws an exception after intermediate saves
- nested-transaction rejection (`BeginTransactionAsync` and `ExecuteInTransactionAsync` when a transaction is already active)
- pre-commit transient failure retry under `EnableRetryOnFailure()`
- post-commit ambiguous connection-drop recovery without re-executing the operation (`TransactionVerificationTracker` for inserted and modified entities, and custom `verifySucceeded` callbacks)
- fail-safe exception throwing without duplicate execution when both commit confirmation and post-commit verification fail

## 8. Concurrency testing

Concurrency scenarios use concurrent tasks such as `Task.WhenAll`.

Covered workflows include:

- first-license issuance
- renewal
- replacement
- detention
- release
- international license
- test result
- test appointment
- local driving-license application

The tests verify that competing operations do not create conflicting business state.

## 9. Critical test-result scenarios

Coverage includes:

- missing appointment
- locked appointment
- future appointment
- invalid test order
- duplicate result
- rollback
- concurrent submission

## 10. HTTP error/status coverage

API tests cover relevant:

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 409 Conflict
- 499 Client Closed Request (implemented by `GlobalExceptionHandler` when `exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested`)
- 500 Unexpected failure

ProblemDetails behavior is also tested at the API boundary where applicable.

## 11. Authentication in integration tests

Production authentication uses JWT.

Most controller and workflow integration tests use `TestAuthenticationHandler` with controlled headers:

```text
X-Test-User-Id
X-Test-Username
X-Test-FullName
X-Test-Role
```

This allows authorization policy behavior to be tested deterministically across endpoints.

In addition, `AuthControllerTests` (`Test/API.IntegrationTests/Controllers/AuthControllerTests.cs`) includes two dedicated tests (`JwtBearer_WhenTokenIsValidAndUserIsActive_AllowsAccessViaUserService` and `JwtBearer_WhenUserIsInactiveOrMissing_ReturnsUnauthorizedViaUserService`) that configure `JwtBearerDefaults.AuthenticationScheme`, mint a real JWT via `IJwtTokenService`, and exercise the full `JwtBearerHandler` cryptographic validation and `OnTokenValidated` active-user verification (`IUserService.IsUserActiveAsync`).

## 12. Run all tests

From the repository root, specify the solution file inside `Presentation (WPF)/`:

```powershell
dotnet test "Presentation (WPF)\DVLD_WPF.sln" --configuration Release
```

Individual suites:

```powershell
dotnet test ".\Test\Application.UnitTests\Application.UnitTests.csproj" --configuration Release

dotnet test ".\Test\API.IntegrationTests\API.IntegrationTests.csproj" --configuration Release

dotnet test ".\Test\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj" --configuration Release
```

## 13. Test inventory and verification status

### Current source & VSTest inventory (commit `4f75658`)

| Project | Test Files | Source Methods (`[Fact]+[Theory]`) | Source Test Cases (`[Fact]+[InlineData]`) | VSTest Discovered Entries (`--list-tests` on `Debug` DLLs) | Audit Execution Outcome |
|---|---:|---:|---:|---:|---|
| `Application.UnitTests` | 21 | 603 | 642 | 633 | Executed via `dotnet test --no-build`: **633 Passed, 0 Failed, 0 Skipped** (~5 s) |
| `API.IntegrationTests` | 33 (29 suites + 4 infra) | 362 | 442 | 435 | Discovered via `--list-tests` (`435` entries); not executed against live SQL Server during audit |
| `Infrastructure.IntegrationTests` | 18 (17 suites + 1 fixture) | 469 | 507 | 494 | Discovered via `--list-tests` (`494` entries); not executed against live SQL Server during audit |
| **Total** | **72 files (67 suites)** | **1,434** | **1,591** | **1,562** | `Application.UnitTests` passed; integration suites enumerated only |

> **Note on counts and build verification:** Source test case counts (`[Fact] + [InlineData]` = `1,591`) are higher than the VSTest-discovered entries (`1,562`) because the existing `bin/Debug/net8.0` assemblies used by `--no-build` were built on 2026-10-26 (12:39–15:21), before commits `b5dc945`, `3056f88`, and `61cbdcc` added 29 source test cases. Source counts are static attribute counts, not executed test results; only the `633` `Application.UnitTests` entries were actually executed. Discovered counts (`435` and `494`) represent test discovery only and are not claimed as passing test runs. During the audit inside a network-isolated sandbox, `dotnet build` failed (`NU1900` NuGet vulnerability check / sandbox MSBuild restriction), so `--no-build` execution and discovery ran against the existing `bin/Debug/net8.0` assemblies.

### Historical full test run result

The latest complete full-suite result reported during earlier development was:

| Project | Passed | Failed | Skipped | Reported duration |
|---|---:|---:|---:|---:|
| Application.UnitTests | 631 | 0 | 0 | ~4.5 s |
| API.IntegrationTests | 364 | 0 | 0 | ~17 s |
| Infrastructure.IntegrationTests | 492 | 0 | 0 | ~1081 s |
| **Total** | **1487** | **0** | **0** | — |

## 14. CI test flow

The Azure DevOps pipeline (`azure-pipelines.yml`) provides Continuous Integration for `.NET` projects (`"Presentation (WPF)/DVLD_WPF.sln"`) through:

```text
Restore
  ↓
Release Build
  ↓
dotnet test
  ↓
Publish Test Results
```

`azure-pipelines.yml` does not currently include build or lint steps for `ClientApp/` (React), and no production deployment stage is claimed.

## 15. Test design principles

For important workflows prefer coverage of:

1. happy path
2. validation failures
3. not-found cases
4. conflict cases
5. authorization failures where applicable
6. rollback
7. concurrency where applicable
8. database constraints where applicable

Critical workflows should be tested at more than one layer when behavior crosses architectural boundaries.
