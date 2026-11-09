# DVLD SQL Server Test Environment Preparation

**Status:** SQL endpoint remains blocked; fixture isolation hardening implemented, but no database connection or modification occurred  
**Date:** 2026-11-09  
**Scope:** Non-destructive inspection and isolation readiness only. No database, backup contents, or credentials were opened.

## Findings

| Requested evidence | Result |
|---|---|
| SQL Server instance | The local SQL Server registry lists the default instance name `MSSQLSERVER` (service instance ID `MSSQL17.MSSQLSERVER`). This identifies an installed instance, but does not establish that it is running, reachable, approved for this task, or safe to use. `Get-Service` could not query Service Control Manager due to access denial. |
| Test connection | `DVLD_TEST_CONNECTION` is absent from the current process environment. No alternative or application connection string was used. No host, credentials, or connection string were guessed. |
| LocalDB | `SqlLocalDB.exe` is installed, but `sqllocaldb info` failed while reading LocalDB instance registry configuration. No LocalDB instance could be verified. |
| DVLD database metadata/access | Not inspected. Without an explicitly approved connection, the existence, accessibility, schema, SQL Server version, and migration compatibility of `DVLD` cannot be established. No SQL connection was attempted. |
| Backup candidate | A file named `D:\SQLData\DVLD.bak` exists (11,657,216 bytes; last-write time 2026-10-25 22:21:10 UTC). Only filesystem metadata was read. Its SQL backup header, integrity, source database identity, and restore compatibility were not verified. It is therefore a candidate file, not a verified backup. |
| `DVLD_Test` | Not created. Source suitability, backup validity, instance identity, and available permissions are unverified. |
| Permissions | Unknown. Service enumeration was denied and no SQL login was attempted. No permission was bypassed or elevated. |
| Protected database | `DVLDf` was not connected to or queried, and no database operation was performed against it. No database or backup file contents were opened. |

## Existing integration-test isolation

`Test/Infrastructure.IntegrationTests/Fixtures/SqlServerTestDatabase.cs` reads only `DVLD_TEST_CONNECTION`, fails if it is missing, generates `DVLD_IntegrationTests_<GUID>`, and substitutes that name as `InitialCatalog`. It runs migrations against that connection and deletes the generated name through an administrative `master` connection during teardown.

The current fixture does **not** reject `AttachDBFilename`, reject an input catalog naming `DVLD` or `DVLDf` before replacing it, verify the server against an explicitly approved instance, or verify database ownership before teardown. The API integration-test factory has a separate implementation with the same missing `AttachDBFilename` and server-identity checks. These are static code findings; neither fixture was run.

## Required changes before fixture implementation (superseded by the corrective pass below)

The existing fixture architecture can be hardened, but a safe target cannot be established with the current environment. Before changing either fixture or running it, the following design inputs are required:

1. Provide `DVLD_TEST_CONNECTION` for a dedicated, approved test SQL Server endpoint. Configure a separate non-secret expected-instance identifier (for example, a documented `DVLD_TEST_SQL_INSTANCE` value) so the fixture can compare the configured server target before opening any connection. Do not provide credentials in this report.
2. Use a test principal limited to the approved test instance and test-database lifecycle. The current fixture creates a database, applies migrations, then drops it, which requires elevated database lifecycle permissions. If these permissions cannot be scoped safely, use a separately approved pre-provisioned isolated test database strategy instead of granting broad server privileges.
3. Update both Infrastructure and API test database factories to reject `AttachDBFilename`, reject unsafe source catalogs (`DVLD`, `DVLDf`, and `master`), verify the expected server target, use generated names, and make cleanup conditional on a current-run ownership token/registry rather than name alone. Keep all setup and cleanup connection paths explicit; never use application configuration as fallback.
4. Add connection-string validation tests that do not open a database, then verify instance identity and account permissions against the approved endpoint before any create/restore/drop operation.

These changes are not implemented here because the required instance approval, connection, and permission scope are unavailable. The user's instruction requires reporting the required changes before implementing them when fixture architecture cannot yet establish the guarantees.

## Blocker and next action

The precise blocker is the absence of a verified test connection and an approved server identity. Local instance discovery is incomplete (instance registry entry exists, service status is inaccessible, and LocalDB enumeration failed). Consequently, no read-only inspection of `DVLD`, backup verification, restore, migration compatibility check, permission check, fixture validation against a real endpoint, or `DVLD_Test` creation can safely proceed.

Provide an approved `DVLD_TEST_CONNECTION` and expected SQL Server instance identifier, plus confirmation of the test principal's allowed database scope. Then first harden and statically test both fixtures, verify the endpoint and permissions, and only afterward validate the `DVLD.bak` header/integrity or inspect `DVLD` metadata. Create `DVLD_Test` only if one of those sources is suitable and the resulting database is proven independent of the original files.

## Follow-up: local instance verification and fixture hardening (2026-11-09)

### Instance and authentication

- Registry metadata identifies the installed default instance as `MSSQLSERVER` (`MSSQL17.MSSQLSERVER`) and its configured static TCP port as `1433`.
- No `sqlservr` process was visible. A TCP probe to `127.0.0.1:1433` failed. Service status could not be queried because Service Control Manager access was denied. The instance is therefore **not verified reachable**; no permission restriction was bypassed.
- Registry `LoginMode=1` indicates Windows Authentication only. The current process token did not report itself as an authenticated Windows identity, so authorization of this account on SQL Server is unknown.
- `DVLD_TEST_CONNECTION` and `DVLD_TEST_EXPECTED_INSTANCE` are not configured. No connection string was set.

When the SQL Server service is available and an authorized Windows account is used, the intended structure is:

```text
DVLD_TEST_CONNECTION=Server=localhost,1433;Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Intent=ReadWrite
DVLD_TEST_EXPECTED_INSTANCE=<SERVERPROPERTY('ServerName')>|<@@SERVICENAME>
```

This is a template only; it was not applied or tested. The expected-instance value must be copied from the live approved instance query below, and the service must first pass a TCP reachability check.

### Fixture changes

Both `SqlServerTestDatabase` and `SqlServerApiWebApplicationFactory` now use shared validation/lifecycle code in `Test/Shared/SqlServerTestEnvironment.cs`. It requires explicit connection and expected-instance settings; requires Windows Authentication for the currently Windows-only instance; rejects credentials, `AttachDBFilename`, user-instance/failover redirection, and source catalogs `DVLD`/`DVLDf`; verifies the live SQL Server identity on an administrative `master` connection before create/drop; and creates a new GUID database rather than reusing an existing one. Each created test database receives a random ownership marker; cleanup drops only a generated test name whose marker matches this execution. Migrations target that generated database only. API integration-host configuration continues to point exclusively to its generated database.

Added database-free connection validation tests in `Test/Infrastructure.IntegrationTests/SqlServerTestEnvironmentValidationTests.cs`. The Infrastructure and API integration-test projects both compiled successfully using direct MSBuild with project-reference builds disabled (exit code 0). A filtered VSTest invocation was started for the validation test class, but it remained at test startup without reporting any cases or a summary and was interrupted. **No unit-test pass is claimed.** `dotnet --info` reported Service Control Manager access denied; `dotnet restore` stopped after “Determining projects to restore” with exit code 1 and no further diagnostic. Direct MSBuild was able to compile both test projects. No unfiltered integration-test suite or fixture lifecycle was intentionally run, and no SQL test connection was opened.

### Need for `DVLD_Test` and backup

Infrastructure tests initialize schema through EF migrations and use explicit test seeding; API integration tests also migrate an isolated database and seed their scenarios. They do not depend on existing `DVLD` records. A separate `DVLD_Test` clone is therefore **not needed** for the current integration-test setup. `D:\SQLData\DVLD.bak` remains an unverified candidate from filesystem metadata only; it was not opened or inspected because no realistic-record dependency was found. No restore was attempted.

### Required manual steps and remaining blockers

1. In Windows Services or SQL Server Configuration Manager, start `SQL Server (MSSQLSERVER)` if it is stopped; do not change permissions to bypass the current restriction. Confirm TCP/IP is enabled on the configured port and have an authorized administrator verify the listener.
2. In SSMS, select **Windows Authentication**, connect to the verified local instance, and run this read-only identity query in `master`:

   ```sql
   SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ServerName'))
          + N'|' + CONVERT(nvarchar(128), @@SERVICENAME) AS ExpectedInstance;
   ```

   Provide the resulting identity and confirm the intended Windows test account is authorized for this endpoint. The account must have only the approved lifecycle privileges needed to create a fresh generated database, apply its migrations, and drop only its own test database. No broad permission should be granted merely to make tests pass.
3. Configure `DVLD_TEST_CONNECTION` and `DVLD_TEST_EXPECTED_INSTANCE` for the test process using the template above, with the verified host/port and query result. Keep credentials out of reports and logs.
4. Re-run only the database-free validation test class first. Then separately approve and verify the isolated database lifecycle before any integration test, migration, restore, or profiling run.

**Final confirmation:** No database was connected to, created, restored, migrated, altered, or dropped. `DVLD` was not modified, and `DVLDf` was not accessed or modified. No backup contents were opened. No N1-01 profiling was run. No commit or push was performed. Source changes are limited to test fixture isolation and its validation tests; this report documents the remaining endpoint and test-run blockers.

## Corrective review and verification (2026-11-09)

### Defects corrected

- Marker installation now runs `USE [generated-name]` inside parameterized `sp_executesql`; the nested SQL context ends with that execution. The outer lifecycle connection stays on `master`, and this is checked after marker creation.
- If marker installation fails, cleanup opens a fresh connection from the explicitly validated admin connection string, revalidates the expected SQL instance, and only then attempts cleanup. Cleanup failure is recorded with the generated database name and exception types only; `ExceptionDispatchInfo` rethrows the original marker failure. Diagnostic listener failures are swallowed so they cannot replace the original exception.
- The create operation records both `database_id` and `service_broker_guid` immediately after `CREATE DATABASE`. Ordinary teardown now requires all of: an approved generated name, matching database ID, matching database identity, matching execution token, and the expected SQL instance. A generated name alone is not sufficient.
- A create collision is rejected and never reused. Cleanup returns an explicit result; both fixtures throw if ownership cannot be confirmed or drop is not confirmed. Marker-failure cleanup may omit the marker only because that same operation successfully created the database, and still requires its captured ID and identity. If that identity cannot be captured, the database is left untouched and a safe diagnostic is recorded.
- Name generation accepts exactly `DVLD_IntegrationTests_` and `DVLD_IntegrationTests_Api_`; validation requires a 32-hex GUID suffix and enforces SQL Server's 128-character identifier limit. Invalid prefixes and malformed names have database-free unit coverage.
- Connection validation continues to require explicit `DVLD_TEST_CONNECTION` and `DVLD_TEST_EXPECTED_INSTANCE`, Windows Authentication, no SQL credentials, no `AttachDBFilename`, user instance or failover, and no `DVLD`/`DVLDf` source catalog. Lifecycle connections are normalized to `master` with ambient enlistment disabled. No application configuration fallback exists.

### Test and build results

- Infrastructure integration-test project build: **passed**, direct MSBuild exit code 0.
- API integration-test project build: **passed**, direct MSBuild exit code 0.
- Filtered database-free validation/naming test execution: **blocked before tests ran**. VSTest launched `testhost.exe`, but the host's communication callback failed with Windows `SocketException (10013)`: “An attempt was made to access a socket in a way forbidden by its access permissions.” The test host never delivered test cases or a summary. The invocation was stopped; **no test pass is claimed**. The diagnostic log was inspected and removed after recording the cause.
- No SQL connection or integration fixture setup/teardown was invoked. The SQL Server endpoint remains unreachable and neither environment variable is set.

### Minimum SQL permissions (not granted or changed)

For the current direct lifecycle design, the narrowest documented database-creation permission is `CREATE DATABASE` in `master`; Microsoft also documents broader alternatives (`CREATE ANY DATABASE` or `ALTER ANY DATABASE`). Do not grant those broader alternatives, `dbcreator`, `CONTROL SERVER`, or `sysadmin` for this task. Microsoft states that the user who creates a database becomes its owner; that owner can run the migrations and owns the generated database. Microsoft documents `CONTROL` on a database as sufficient for `DROP DATABASE`; ownership therefore covers teardown of the database created by that principal. See [Create a database — Microsoft Learn](https://learn.microsoft.com/en-us/sql/relational-databases/databases/create-a-database) and [Delete a database — Microsoft Learn](https://learn.microsoft.com/en-us/sql/relational-databases/databases/delete-a-database).

**Limit:** SQL Server's built-in `CREATE DATABASE` permission is not name-scoped. Application-side GUID/prefix checks protect these fixtures but do not prevent that principal from issuing another create command outside the fixture. If the security requirement is that the principal be technically unable to create any other database, a DBA-controlled provisioning wrapper/service or a pre-provisioned isolated-database design is needed; neither is implemented or approved here. No permissions were granted or altered.

### Remaining blockers and safety confirmation

The SQL Server listener is still unreachable, Windows Service Control Manager access is denied, and the current process does not have a verified authenticated Windows SQL identity. The SSMS/manual steps in the prior section remain required before setting either environment variable. No database was connected to, created, restored, migrated, altered, or dropped. `DVLD` was not modified; `DVLDf` was not accessed. No N1-01 profiling, integration tests, permission changes, commit, or push occurred.

## VSTest Socket 10013 investigation (2026-11-09)

### Environment and runner versions

- Repository `global.json` selects .NET SDK `8.0.427` (roll-forward `latestFeature`); SDK `10.0.403` is also installed.
- Installed runtimes include `Microsoft.NETCore.App 8.0.33` and `10.0.14`; the test projects target `net8.0` and the captured host log reports runtime `8.0.33`.
- Infrastructure tests use `Microsoft.NET.Test.Sdk 17.8.0`, testhost `17.8.0`, and xUnit VS adapter `2.5.3`. API tests use SDK/testhost `17.12.0` and adapter `2.8.2`.
- SDK VSTest console was `17.11.1`. Visual Studio Community 18 is installed, with VSTest console `18.11.0`.
- No `dotnet`, `testhost`, or `vstest.console` process was present during the process check; no competing testhost was identified.

### Cause assessment

The VSTest diagnostic log shows the runner successfully bound a listener on `127.0.0.1` and started `testhost.exe`. The host connected to the runner endpoint, then failed in `TestRequestHandler.InitializeCommunication` with `SocketException (10013)` before test-case discovery or execution. The same test-host handshake failed using the Visual Studio 18.11.0 runner, so changing only the VSTest console version did not resolve it.

Separately, a minimal PowerShell loopback TCP diagnostic could start a listener but its `TcpClient.ConnectAsync` was rejected with the same `SocketException (10013)`. This demonstrates a local socket-permission restriction in the current execution environment independent of the DVLD test code. It is not evidence that a particular firewall or endpoint-security product caused the restriction.

Firewall attribution could not be established: `Get-NetFirewallProfile`/rule inspection failed with “Access to a CIM resource was not available to the client”; `netsh advfirewall` was unavailable in this shell. No firewall or endpoint-security setting was changed. Therefore the established root cause is **loopback socket access denied in this environment**; the specific enforcement layer (Windows policy, firewall, endpoint security, or host sandbox policy) remains unknown. SDK/testhost version skew exists between Infrastructure and API projects, but is not established as the cause because a standalone loopback connect also fails and VSTest 18.11.0 reproduced the same socket error.

### Safe alternatives evaluated

- Ran the filtered test class with SDK VSTest `17.11.1`; the host failed before discovering tests.
- Ran the same filter with Visual Studio VSTest `18.11.0`; it failed at the same loopback communication stage. No test ran.
- VSTest's supported `/Port` option configures the port for its socket connection; it does not replace the socket transport. Changing ports, disabling security controls, or requesting elevated permissions would not be an acceptable workaround. Microsoft documents the socket port option in [VSTest console options](https://learn.microsoft.com/en-us/visualstudio/test/vstest-console-options?view=vs-2019).
- No `Microsoft.NET.Test.Sdk` upgrade was made. The Infrastructure 17.8.0 versus API 17.12.0 difference should be aligned through a separately approved dependency change, but the socket policy must first be addressed by the environment owner. No test adapter, MTP migration, or alternate runner package was added.
- Visual Studio is installed, but its command-line VSTest runner encounters the same restriction. An interactive Developer PowerShell/Visual Studio session running outside the restricted host may work only if that environment permits local loopback; this was not assumed or claimed.

### Database-free test result

`SqlServerTestEnvironmentValidationTests` contains **21 source-level cases** (including theory rows) covering connection checks, both approved prefixes, invalid prefixes/names, and generated test-database connection strings. VSTest never completed discovery, so actual runner-discovered total is unavailable.

| Result | Count |
|---|---:|
| Passed | 0 |
| Failed test assertions | 0 |
| Skipped | 0 |
| Executed | 0 |
| Discovered by runner | Not reported; host handshake failed first |

This is a test-runner startup block, not a passing test run or an assertion failure. No integration tests ran and no SQL connection was attempted. The diagnostic files used to establish the cause were removed after inspection.

### Remaining actions

1. Have the Windows/endpoint-security administrator inspect the active socket policy for the current execution host and permit the VSTest runner/testhost loopback communication required for tests. Do not disable the firewall or endpoint protection and do not grant broad admin rights.
2. Alternatively, run the filtered command from an authorized Developer PowerShell or Visual Studio session where the local loopback diagnostic succeeds; first confirm the session's .NET SDK and testhost versions. Then run only `SqlServerTestEnvironmentValidationTests` and retain the complete test summary.
3. Separately, SQL Server `MSSQLSERVER` remains unreachable on its configured `127.0.0.1:1433` endpoint. The earlier SSMS instructions remain applicable; no service or network setting was changed in this task.

**Final safety confirmation for this investigation:** No SQL connections, database operations, migration, profiling, permission changes, firewall changes, commits, or pushes occurred. `DVLD` and `DVLDf` remain untouched.

## AttachDBFilename false-positive correction (2026-11-09)

### Cause and correction

`SqlServerTestEnvironment.ValidateConfiguration` initially inspected `SqlConnectionStringBuilder.Keys`, which can include supported keywords even when omitted. A first correction switched to `SqlConnectionStringBuilder.ContainsKey`, but that builder also reports supported keywords that were not supplied. That intermediate correction was itself incorrect and has now been superseded by the explicit-key implementation in the later section, using `DbConnectionStringBuilder` against the original string. Regression cases cover exact, lowercase, and mixed-case spellings; existing cases cover Windows Authentication, SQL credentials, `DVLD`/`DVLDf`, failover/user-instance redirects, and normalization to `master`.

### Previous Visual Studio failures

The only prior failure details available in this workspace are the user's Test Explorer summary (21 total, 16 passed, 5 failed) and the common exception message `DVLD_TEST_CONNECTION must not include AttachDBFilename.` No Test Explorer detailed output, `.trx`, or diagnostic artifact containing the five full test names and individual assertions/exceptions was available for inspection. Therefore the five failures cannot responsibly be listed as individually confirmed or claimed all to share this root cause. The current source fix addresses the confirmed false-positive mechanism; the previous individual failures remain unverified pending the detailed Test Explorer output.

### Current build and targeted run

- Infrastructure integration-test project: **build passed** with Visual Studio MSBuild 18.11.1, exit code 0; output assembly was produced at `Test/Infrastructure.IntegrationTests/bin/Debug/net8.0/Infrastructure.IntegrationTests.dll`.
- A `dotnet build --no-restore` attempt exited 1 but emitted no diagnostic beyond `Build FAILED` (0 warnings, 0 errors); the Visual Studio MSBuild result above is the successful project build.
- The Visual Studio VSTest console 18.11.0 accepted the assembly and exact `FullyQualifiedName~SqlServerTestEnvironmentValidationTests` filter, but failed to connect to `testhost.exe` after 90 seconds. It aborted before reporting discovery or test results. **No test execution or pass is claimed.** Source currently defines 23 cases when theory rows are expanded; runner-discovered count is unavailable.
- No database connection or fixture lifecycle operation occurred. SQL Server reachability remains blocked as documented above.

### Remaining blockers

1. Have the authorized Windows/endpoint-security owner resolve the previously evidenced loopback socket restriction for VSTest/testhost, without disabling security controls. Then run the filtered class in the user's working Visual Studio Test Explorer and retain the complete per-test failure list and summary.
2. Reconcile the previous five detailed failures from that retained output before asserting that all five were fixed.
3. SQL Server remains unreachable; this task did not attempt to connect or alter any SQL Server or database state.

**Safety confirmation for this correction:** no SQL connection, integration test, database creation/change, firewall/security change, commit, or push occurred. `DVLD` remains unmodified and `DVLDf` was not accessed.

## Explicit connection-string key detection correction (2026-11-09)

### Correction and regression coverage

The `SqlConnectionStringBuilder.ContainsKey("AttachDBFilename")` check was not a reliable test for explicit presence: its fixed keyword collection can report supported keywords that were omitted. Validation now first parses the original string with `System.Data.Common.DbConnectionStringBuilder` and checks `ContainsKey("AttachDBFilename")` there. It then independently parses that same original string with `SqlConnectionStringBuilder` for SQL Server-specific checks. The prohibited-key check remains case-insensitive and rejects the key even when its value is empty.

Regression coverage now includes a normal SQL Server connection string without the key, explicit key spellings in original/lower/mixed case, an explicit empty value, and a direct generic-builder test confirming an omitted key is absent while a supplied key is present. Existing tests for Windows Authentication, SQL credentials, protected catalogs, failover/user-instance redirection, and `master` normalization remain in place.

### Build and requested Test Explorer run

- Infrastructure integration-test project build: **passed**, Visual Studio MSBuild 18.11.1 exit code 0; output assembly produced.
- Test Explorer execution: **not performed**. The running Visual Studio window was present, but its accessibility state was unavailable to the UI control interface, so I could not interact with its Test Explorer or obtain its result pane. Per instruction, I did not substitute the restricted agent VSTest runner, which previously failed at testhost loopback communication.
- Actual discovered and executed counts: **unavailable; no tests were run in this pass**. Although the source currently declares 26 cases after expanding theory rows, that is a source-derived count and is not a Test Explorer discovery/execution result.
- The earlier 21-test/16-pass/5-fail output remains the only prior summary provided. No new pass/fail summary is available.

No database connection, integration test, migration, or lifecycle operation was attempted. SQL Server reachability remains a separate unresolved blocker. `DVLD` was not modified, `DVLDf` was not accessed, and no firewall, permission, commit, or push action occurred.

## Least-privilege integration-test provisioning review (2026-11-09)

### Scope and evidence boundary

This is a read-only code review. The supplied instance identity (`Ayhm|MSSQLSERVER`) and test login (`AYHM\DVLD_TestRunner`) permissions are user-provided context; they were not independently verified because this review made no SQL connection. `DVLDf` was not accessed. No test or database lifecycle operation was run.

### Current fixture lifecycle

| Area | Infrastructure integration tests | API integration tests |
|---|---|---|
| Configuration and target | `SqlServerTestDatabase.InitializeAsync` reads only `DVLD_TEST_CONNECTION` and `DVLD_TEST_EXPECTED_INSTANCE`; shared validation normalizes the admin connection to `master`. | `SqlServerApiWebApplicationFactory` constructor calls `InitializeDatabase`, using the same shared validation. |
| Create | Generates `DVLD_IntegrationTests_<GUID>` and calls `SqlServerTestEnvironment.CreateDatabaseAsync` from `master`; the helper verifies instance/name nonexistence, creates the DB, records database ID and service-broker GUID, then adds a random run marker. | Same flow with `DVLD_IntegrationTests_Api_<GUID>`. |
| Migrate | Builds an EF `DVLDDbContext` against that generated database and calls `Database.MigrateAsync()`. A migration failure attempts owned-database cleanup while preserving the migration exception. | Builds the test context and calls `Database.Migrate()`; a static in-process `SemaphoreSlim(2,2)` limits concurrent API lifecycle work in this process. A migration failure invokes cleanup. |
| Seed/use | Test classes compose `SqlServerTestDatabase` through class fixtures and create contexts. Seed rows are generally inserted by test setup/helpers with EF `Add`/`AddRange` and `SaveChanges`; the fixture itself does not populate a common baseline seed. | SQL-backed workflow tests instantiate the factory and seed scenario data through created contexts and `SaveChanges`. The separate `ApiWebApplicationFactory` used by mocked controller tests replaces services and does not use SQL. |
| Drop | `DisposeAsync` calls `DropOwnedDatabaseAsync` with the lease. Shared cleanup requires approved generated name, matching database ID/identity, matching run token, and expected instance before dropping. | Both `DisposeAsync` and synchronous `Dispose(bool)` can invoke cleanup; the lease is exchanged once, the pool is cleared, and the shared ownership checks are applied. |

The source does not use `EnsureDeleted`/`EnsureCreated` for this lifecycle. It uses EF migrations and per-test/per-class scenario seeding. The exact number of generated databases depends on which test classes/workflows run and their fixture scope.

### Permission assessment

The login already has the required `CONNECT SQL` server permission according to the supplied context. For the existing dynamic lifecycle, SQL Server documents `CREATE DATABASE` permission in `master` as the narrowest of the documented alternatives; `CREATE ANY DATABASE` and `ALTER ANY DATABASE` are broader alternatives and should not be used here. The principal that creates a database becomes its owner. That ownership is sufficient to run the EF migrations, seed and manipulate test data, and drop its own generated database; no `sysadmin`, `dbcreator`, `CONTROL SERVER`, or server-wide `ALTER ANY DATABASE` membership is required. Microsoft documents the create requirement in [Create a database](https://learn.microsoft.com/en-us/sql/relational-databases/databases/create-a-database) and that `db_owner` can drop a database in [Database-level roles](https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/database-level-roles?view=sql-server-ver17).

Important limitation: `CREATE DATABASE` is not name-scoped. Granting it allows this login to attempt creation of databases beyond the fixture's approved prefixes. The application checks protect normal fixture use and prevent the fixture from targeting `DVLD`/`DVLDf`; they do not technically constrain a compromised or repurposed credential to a naming prefix. The permission does not itself grant access to existing `DVLD` or `DVLDf`; keep the test login absent from those databases and do not grant server-wide roles.

For a pre-provisioned design, the login would need a database user and migration-capable rights in every test database. A practical simple grant is `db_owner` scoped only to those dedicated test databases, which supports migration and seeding but also lets the member drop those databases. A tighter custom role would require deriving and maintaining all permissions needed by the current EF migration history and test operations; no evidence currently establishes a smaller stable set. Do not map the test login as owner/member in either protected application database.

### Pre-provisioned databases vs. controlled provisioning

- **Fixed pre-provisioned DBs:** avoids giving the test login database-creation permission, but does not match the current fixtures' isolation contract. Infrastructure class fixtures and API workflow factories expect fresh GUID databases; reusing fixed databases risks stale rows, collisions, cross-test interference, and concurrent runs sharing mutable state. Adapting this safely requires a lease/pool model with one isolated database per concurrent fixture/run plus a proven reset strategy, along with fixture changes to stop creating/dropping and to validate the assigned database. A single shared `DVLD_Test` is not safe for the current parallel test patterns without serializing and reliably resetting every scenario.
- **Administrator-controlled provisioner:** keeps create/drop rights out of the test login, but requires a separate trusted service, operator step, or carefully secured signed SQL module to allocate isolated per-run databases and return leases, then revoke/remove them. The fixtures must call that provisioning interface instead of directly creating/dropping databases. This has more operational components and failure modes than needed for one local development instance.
- **Current ephemeral fixture lifecycle with narrow create grant:** preserves database-per-fixture isolation, current migration/seeding behavior, GUID ownership lease, and cleanup checks. Its tradeoff is the non-name-scoped ability to create databases. On this local instance, where the test login has no stated fixed server roles and must remain unprivileged in `DVLD`/`DVLDf`, this is the simplest practical option if the operator accepts that bounded provisioning capability.

### Recommendation and implementation plan (not implemented)

Recommend retaining the existing ephemeral database-per-fixture design and granting only `CREATE DATABASE` in `master` to `AYHM\DVLD_TestRunner`, in addition to its existing `CONNECT SQL`, after an authorized DBA reviews and approves the non-name-scoped creation risk. Do not grant `dbcreator`, `CREATE ANY DATABASE`, `ALTER ANY DATABASE`, `CONTROL SERVER`, or `sysadmin`. Do not add the login to `DVLD` or `DVLDf`. The creator-owned generated DB supplies the scoped authority required by the current migrations, seed operations, and teardown.

Implementation sequence after explicit approval:

1. DBA verifies the named Windows login and current effective permissions on the verified instance, confirms it has no user/role access to `DVLD` or `DVLDf`, and applies only the approved `CREATE DATABASE` permission in `master` (no changes are made by this review).
2. Keep `SqlServerTestEnvironment` expected-instance validation, protected-catalog rejection, generated name validation, identity/run-token tracking, and ownership-checked cleanup. Add a preflight/read-only diagnostic that clearly reports whether the test principal can perform the required lifecycle without creating a database (permission metadata check where available); it must not fall back to application credentials.
3. Review cleanup error propagation and the API lifecycle semaphore against expected local parallel test load; do not weaken lease checks or make fixture databases reusable. Consider a cross-process migration throttling/coordination mechanism only if concurrent execution profiling demonstrates a real conflict; the existing static semaphore coordinates one process only.
4. With separate approval and a verified isolated endpoint, run a small isolated fixture smoke test and confirm it creates, migrates, seeds, and removes only its generated database. Do not run that validation as part of this read-only review.

No implementation, permission change, SQL connection, integration test, migration, database create/drop, commit, or push was performed. The previously reported endpoint state is superseded only by the user's current claim of reachability; it has not been independently verified in this task.

## API Integration Factory Cleanup and Build Verification (2026-11-09)

### Code changes

The API integration-test factory received the following lifecycle corrections:

1. **Migration lock release:** `InitializeDatabase` now releases `DatabaseLifecycleLock` in a `finally` block before attempting database cleanup after a migration failure. This prevents concurrent migration failures from each holding a semaphore permit while waiting to acquire another permit for cleanup.
2. **Asynchronous disposal:** `DisposeAsync` now attempts database cleanup even if `base.DisposeAsync()` throws. The original disposal exception is preserved and rethrown. If cleanup also fails, the cleanup failure is reported safely without replacing the primary exception.
3. **Synchronous disposal:** `Dispose(bool)` applies equivalent cleanup handling when `disposing` is true. The finalizer path (`disposing == false`) delegates to the base implementation without attempting database cleanup.

The existing database lease, connection-pool clearing, ownership validation, and shared cleanup mechanism remain in use.

### Static verification

- API integration-test project build using `dotnet build .\Test\API.IntegrationTests\API.IntegrationTests.csproj --no-restore`: **PASSED**.
- Build result: 0 warnings, 0 errors.
- `git diff --check`: no whitespace errors reported.
- Git emitted LF-to-CRLF normalization warnings for the three modified files. These are line-ending warnings, not whitespace-check failures.

### Runtime verification status

The changes have not been verified through runtime integration tests. In particular, synchronous and asynchronous disposal behavior, migration-failure cleanup, and concurrent lifecycle behavior remain untested.

No integration tests were executed, no SQL connection was attempted, and no database lifecycle operation was performed during this verification.

### Safety confirmation

The changes are limited to test infrastructure, validation-test source, and audit documentation. No SQL Server permissions, firewall settings, application connection strings, or protected database configurations were changed.

`DVLD` and `DVLDf` remain untouched. No commit or push was performed.
