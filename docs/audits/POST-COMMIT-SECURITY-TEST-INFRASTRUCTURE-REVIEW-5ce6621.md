# DVLD Post-Commit Security & Test Infrastructure Review

**Reviewed commit:** `5ce662155a448d527ef32f5211ecb322bc1a2fb6` (`5ce6621`)
**Review type:** Read-only static code review
**Result:** No SQL connection, test execution, database operation, permission change, or source modification was performed.

## Executive summary

The commit improves API test factory cleanup behavior and moves migration-failure cleanup outside the migration semaphore, avoiding the specific permit-starvation/deadlock pattern where failed migrations held permits while awaiting cleanup permits. The SQL test database lifecycle continues to use unique generated names, server identity checks, database identity tracking, and a per-run marker before ordinary teardown. Static configuration validation rejects `DVLD` and `DVLDf` as input catalogs and then forces lifecycle connections to `master`; downstream data connections are constructed only for validated generated names.

No confirmed path in the reviewed fixtures targets either protected catalog. The principal concerns are around disposal composition and failure recovery: `WebApplicationFactory.DisposeAsync()` invokes the virtual `Dispose(bool)` method, so this subclass's async disposal path also reaches synchronous SQL cleanup; and cleanup consumes the lease before a drop is confirmed, which can leave an orphaned generated database after transient cleanup failure. These are not evidence of access to application databases.

**Overall assessment:** the lifecycle guards are materially safer than name-only cleanup, and the latest lock-order change is directionally correct. No Critical or High confirmed exploit/data-integrity defect was found in the commit. A Medium-risk cleanup/retry weakness and a Medium potential sync-over-async disposal hazard merit follow-up. One explicit-empty-`AttachDBFilename` regression case was removed from the validation tests in this commit.

## Files and components reviewed

- `Test/API.IntegrationTests/Infrastructure/SqlServerApiWebApplicationFactory.cs` — changed by the commit; construction, migration, sync/async disposal and cleanup.
- `Test/Shared/SqlServerTestEnvironment.cs` — shared connection validation, create, marker, identity lease and drop safeguards.
- `Test/Infrastructure.IntegrationTests/Fixtures/SqlServerTestDatabase.cs` — comparison implementation for migration and teardown.
- `Test/Infrastructure.IntegrationTests/SqlServerTestEnvironmentValidationTests.cs` — reviewed test diff; one case was removed.
- Commit metadata and diff for `5ce6621`.

## Findings

| ID | Severity | Status | Location | Finding and impact | Recommended action |
|---|---|---|---|---|---|
| F-01 | MEDIUM | Confirmed control flow; runtime impact conditional | `Test/API.IntegrationTests/Infrastructure/SqlServerApiWebApplicationFactory.cs:77-112,115-157`; base behavior in `WebApplicationFactory.cs` | The override calls `base.DisposeAsync()`, and ASP.NET Core 8 `WebApplicationFactory.DisposeAsync()` calls `Dispose(disposing: true)` before returning. That virtual call dispatches to this class's `Dispose(bool)`, which synchronously invokes `DropDatabase()` (`.GetAwaiter().GetResult()`). The async override then invokes `DropDatabaseAsync()` again; `Interlocked.Exchange` makes the second call a no-op if the first consumed the lease. Thus async disposal can block on synchronous SQL cleanup and has complex nested exception flow. A synchronization-context deadlock is possible when the sync path blocks while awaited cleanup captures that context; this was not runtime-tested and is more likely in a context-bound caller than a normal xUnit worker thread. | Consolidate database cleanup behind a single disposal path that does not perform blocking SQL work from the async path. Add focused tests with an instrumented cleanup abstraction for async disposal, sync disposal, and base-disposal failures; do not test against a real database for these control-flow checks. |
| F-02 | MEDIUM | Confirmed current behavior; failure is conditional | `Test/API.IntegrationTests/Infrastructure/SqlServerApiWebApplicationFactory.cs:224-253`; shared lease helper unchanged at `Test/Shared/SqlServerTestEnvironment.cs:324-343` | `DropDatabaseAsync` atomically clears `_databaseLease` before connection open/ownership check/drop completes. If opening, verification, or drop fails, later disposal calls see no lease and return without retrying. The first cleanup attempt still reports failure, but a database created by this execution can remain orphaned and the same factory cannot retry it. Migration-failure cleanup also catches and logs cleanup exceptions before rethrowing the migration exception (`:206-220`), so the primary failure is preserved while cleanup failure is only diagnostic output. | Retain the lease until cleanup succeeds or ownership is conclusively invalid; preserve idempotence under concurrent disposal. If preserving the primary exception, attach the cleanup failure as secondary diagnostic data or aggregate/report it through a reliable test-runner channel. Any retry must continue checking name, database ID, service-broker identity, marker, and expected instance. |
| F-03 | LOW | Confirmed test-coverage regression in this commit | `Test/Infrastructure.IntegrationTests/SqlServerTestEnvironmentValidationTests.cs:75-88`; commit diff removes `[InlineData("AttachDBFilename=")]` | The empty-value case that previously proved an explicitly supplied `AttachDBFilename` key is rejected regardless of value is removed. The current generic `DbConnectionStringBuilder` implementation should still recognize the explicit key, but that policy edge no longer has regression coverage. | Restore the empty-value theory row. This requires no database access. |
| F-04 | LOW | Potential operational gap, not a data race in current unique-DB design | `Test/API.IntegrationTests/Infrastructure/SqlServerApiWebApplicationFactory.cs:21-22,188-204,224-253` | `DatabaseLifecycleLock` is a `SemaphoreSlim(2,2)`: it permits two lifecycle operations concurrently and coordinates only factories in the same process. It is a throttle, not a mutual-exclusion lock. Separate test processes are not coordinated. Because each factory uses a fresh GUID database, concurrent migrations do not share the same EF migration history; the remaining concern is concurrent SQL Server resource pressure, not cross-test writes to one database. | Keep the two-permit throttle if intentional and document it as such. Add cross-process coordination only if runtime evidence shows SQL resource contention or if future code reuses databases. |

### F-01 framework evidence

The .NET 8 ASP.NET Core `WebApplicationFactory<TEntryPoint>.DisposeAsync()` implementation sets its async-disposal state and then calls `Dispose(disposing: true)` at the end. Because `Dispose(bool)` is virtual, it dispatches to the subclass override. Source: [ASP.NET Core 8 WebApplicationFactory.cs](https://github.com/dotnet/aspnetcore/blob/v8.0.0/src/Mvc/Mvc.Testing/src/WebApplicationFactory.cs#L518-L550).

## Migration lock and deadlock assessment

`InitializeDatabase` acquires the semaphore before `Database.Migrate()`. The latest commit captures the migration exception, releases the semaphore in `finally`, and only then calls `DropDatabase()`; cleanup reacquires the semaphore through `DropDatabaseAsync`. This avoids holding a permit while waiting for another permit during failed migration cleanup. With two permits, this specific self-deadlock/permit-starvation scenario is absent.

The lock capacity is two rather than one, so two migrations and/or lifecycle cleanups may proceed at once in this process. No deadlock is evident from the revised order. The lock does not cover database creation (which occurs before acquisition) and does not coordinate other processes. Those are not currently correctness defects because database names are GUID-based and each factory uses a separate database; cross-process behavior remains unverified.

## Database ownership and cleanup safeguards

Static source review confirms the shared helper:

1. Requires an open administrative connection whose current database is `master` and checks the live server identity before create/drop (`SqlServerTestEnvironment.cs:165-187,195-200,355-360`).
2. Accepts only the approved integration prefixes with a 32-character GUID suffix and a maximum 128-character name (`:420-447`).
3. Checks that the generated name does not already exist and refuses reuse (`:203-221`).
4. Captures `database_id` and `service_broker_guid` after creation (`:224-251`).
5. Adds a random extended-property run marker using parameterized dynamic SQL (`:252-280`).
6. For ordinary cleanup, requires matching database ID, service-broker identity and run-token marker before `ALTER DATABASE ... SINGLE_USER ... DROP DATABASE` (`:346-417`).
7. Allows marker-free cleanup only in the internal marker-creation failure path, where the current create operation has already captured the new database ID and identity (`:282-320`).

The database name interpolation in `CREATE DATABASE` and the drop statement is constrained by strict generated-name validation; SQL values remain parameters. Marker-free recovery is private to creation failure and is not the ordinary teardown route. Cleanup remains fail-closed when marker or identity validation does not match.

One operational caveat: cleanup has no retry/retained lease after a failure (F-02). A retained orphan is safer than dropping an unverified database, but needs a documented/manual cleanup path that validates ownership evidence before removal.

## Protected database isolation assessment

**Confirmed from code:** `ValidateConfiguration` rejects an initial catalog equal to `DVLD` or `DVLDf` case-insensitively (`SqlServerTestEnvironment.cs:83-92`). It then rewrites the administrative connection's catalog to `master` (`:119-121`). `BuildDatabaseConnectionString` validates the generated test name before setting it as the data catalog (`:151-163`). Create and drop operations require the expected server identity and `master` context. Neither fixture embeds a separate application/default connection string.

**Boundary:** this proves the reviewed fixture path does not intentionally address those catalogs. It does not prove effective SQL permissions or the current Windows process identity, because no connection was made. Integrated security uses the process token; the code verifies the SQL Server instance but does not assert the actual login name equals a configured dedicated test principal. Consequently, launching tests under a more privileged Windows identity remains a configuration risk. A future preflight could verify `ORIGINAL_LOGIN()`/`SUSER_SNAME()` against the expected test identity before any create operation, with no access to protected data.

No database was connected to, queried, or modified during this review. `DVLDf` was not accessed.

## Regression assessment of commit `5ce6621`

- **Positive:** migration semaphore release now precedes migration-failure cleanup, eliminating the prior pattern where a failed migration held permits while cleanup waited for one.
- **Positive:** both sync and async disposal attempt cleanup even when base disposal fails, and preserve the primary exception rather than replacing it with a cleanup exception.
- **No confirmed protected-database targeting regression:** shared validation and generated-name rules remain in place and were not changed by this commit.
- **Potential downside:** async disposal enters the synchronous override through the base implementation (F-01); no execution evidence was collected.
- **Coverage regression:** the explicit empty `AttachDBFilename` row was deleted (F-03). The validator code itself was not changed in this commit.

## Verification limits

This was a read-only static review. No integration tests, builds, SQL connectivity checks, fixture constructors, migrations, database setup/teardown, permission checks, or operations against `DVLD`/`DVLDf` were run. The supplied instance/login details were treated as user-provided context, not independently verified.

## Recommended next steps, by priority

1. **MEDIUM — Fix disposal-path composition:** make async cleanup genuinely async and ensure base `WebApplicationFactory` disposal cannot trigger a second/synchronous database cleanup path. Add database-free lifecycle unit tests for disposal call order and exception preservation.
2. **MEDIUM — Preserve cleanup ownership evidence after failure:** avoid clearing a lease before cleanup is confirmed; report cleanup failure alongside the primary migration/disposal exception without weakening ownership checks.
3. **LOW — Restore the empty `AttachDBFilename=` regression row** in `SqlServerTestEnvironmentValidationTests`.
4. **LOW — Clarify/test concurrency intent:** document that the semaphore permits two same-process lifecycle operations; only add cross-process locking if actual parallel runs show a shared-resource problem.
5. **IMPROVEMENT — Add a principal preflight** that verifies the expected SQL login identity before creating any generated database, and fails closed if the process is running as a different Windows account.

**Final safety statement:** No source file was modified or created by this review; only this audit document was created. No SQL connection, integration test, migration, database change, permission change, commit, or push occurred.
