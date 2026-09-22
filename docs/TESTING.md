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

These use `WebApplicationFactory<Program>` with Application services mocked where appropriate.

They verify:

- routing
- HTTP methods
- authorization behavior
- request binding
- response mapping
- status codes
- ProblemDetails mapping
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

UnitOfWork tests cover transaction-related behavior including:

- SaveChanges
- transaction execution
- commit
- rollback

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
- 500 Unexpected failure

ProblemDetails behavior is also tested at the API boundary where applicable.

## 11. Authentication in integration tests

Production authentication uses JWT.

Integration tests use `TestAuthenticationHandler` with controlled headers:

```text
X-Test-User-Id
X-Test-Username
X-Test-FullName
X-Test-Role
```

This allows authorization policy behavior to be tested deterministically without claiming to test production JWT cryptographic validation end to end.

## 12. Run all tests

```powershell
dotnet test --configuration Release
```

Individual suites:

```powershell
dotnet test ".\Test\Application.UnitTests\Application.UnitTests.csproj" --configuration Release

dotnet test ".\Test\API.IntegrationTests\API.IntegrationTests.csproj" --configuration Release

dotnet test ".\Test\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj" --configuration Release
```

## 13. Historical test result

The latest complete result reported during development was:

| Project | Passed | Failed | Skipped | Reported duration |
|---|---:|---:|---:|---:|
| Application.UnitTests | 631 | 0 | 0 | ~4.5 s |
| API.IntegrationTests | 364 | 0 | 0 | ~17 s |
| Infrastructure.IntegrationTests | 492 | 0 | 0 | ~1081 s |
| **Total** | **1487** | **0** | **0** | — |

These are historical user-reported results, not a fresh execution claim for this documentation update.

## 14. CI test flow

The Azure DevOps pipeline provides Continuous Integration through:

```text
Restore
  ↓
Release Build
  ↓
dotnet test
  ↓
Publish Test Results
```

No production deployment stage is claimed.

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
