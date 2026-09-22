# DVLD Testing Guide

## Test projects

- Test/Application.UnitTests
- Test/API.IntegrationTests
- Test/Infrastructure.IntegrationTests

## Unit tests

Focus on validation, Result behavior, business rules, service orchestration, and edge cases.

## API integration tests

Verify HTTP routing, authorization, request/response behavior, and application-stack integration.

## Infrastructure integration tests

Verify EF Core mappings, SQL Server constraints, transactions, foreign keys, indexes, and concurrency.

## Critical scenarios

The test workflow covers cases including missing appointment, locked appointment, future appointment, invalid test order, duplicate result, rollback, and concurrent submission.

## Run all tests

```powershell
dotnet test --configuration Release
```

## Run individual suites

```powershell
dotnet test ".\Test\Application.UnitTests\Application.UnitTests.csproj" --configuration Release
dotnet test ".\Test\API.IntegrationTests\API.IntegrationTests.csproj" --configuration Release
dotnet test ".\Test\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj" --configuration Release
```

## Latest reported run

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Application.UnitTests | 631 | 0 | 0 |
| API.IntegrationTests | 364 | 0 | 0 |
| Infrastructure.IntegrationTests | 492 | 0 | 0 |

These are historical user-reported results, not a fresh execution claim.

## Test database

The infrastructure test fixture uses the `DVLD_TEST_CONNECTION` environment variable.

Never commit production credentials.

## Test design rule

Critical workflows should have happy-path, validation, not-found/conflict, rollback, and concurrency tests where applicable.
