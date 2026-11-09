# CI-SQL-01 — Azure Pipelines SQL Server Test Reliability

> **Date:** 2026-11-09
> **Scope:** Repair the SQL-backed integration-test environment in Azure Pipelines.
> **WPF:** untouched. **`DVLDf`:** never accessed. **Safety validation:** unchanged and fully preserved.

---

## 1. Original failure

Latest pipeline run:

| Suite | Total | Passed | Failed | Failure |
|---|---:|---:|---:|---|
| API.IntegrationTests | 601 | 525 | 76 | `DVLD_TEST_EXPECTED_INSTANCE must identify the approved SQL Server instance.` |
| Infrastructure.IntegrationTests | 656 | 34 | 622 | same |

All failures occurred in fixture initialization (`SqlServerTestDatabase.InitializeAsync`, `SqlServerApiWebApplicationFactory.InitializeDatabase`) before any test body ran. The 525 API passes are the database-free controller/security suites; the 34 infrastructure passes are the database-free validation tests.

## 2. Root cause

**The pipeline never provided `DVLD_TEST_EXPECTED_INSTANCE`.**

Evidence:

1. `azure-pipelines.yml` defines `DVLD_TEST_CONNECTION` (line 304) but contains no `DVLD_TEST_EXPECTED_INSTANCE` anywhere.
2. `eng/scripts/Initialize-TestSqlEngine.ps1` started the run-specific LocalDB instance, verified `master`, and published `SqlProbeDataSource`, ownership variables and `SqlServerReady` — but **never queried or published the instance identity**.
3. Both SQL test tasks (`Run API Integration Tests`, `Run Infrastructure Integration Tests`) passed only `DVLD_TEST_CONNECTION` (and JWT for API).
4. `SqlServerTestEnvironment.ValidateConfiguration` correctly rejects an empty expected identity. This is the intended fail-closed behaviour, not a defect.

Therefore SQL Server *was* available and started; the failure was a **missing configuration contract**, not an unavailable server. This was confirmed by executing the provisioning script locally: it started the instance, connected to `master`, and only lacked identity publication.

The identity is also **not derivable** from the LocalDB alias: the running engine reports `<machine>\LOCALDB#<hash>|<LOCALDB#<hash>` (the hash differs per instance and from `MSSQLLocalDB`). Hardcoding or assuming a value would have been incorrect.

## 3. Selected provisioning architecture

**Keep the existing run-specific SQL Server LocalDB instance** (`DVLD_CI_<BuildId>_<JobAttempt>`) on the `windows-latest` agent. Rationale:

- LocalDB is already present on the hosted Windows image and the repository's ownership/cleanup logic is built and verified around it.
- A SQL Server Linux container cannot run as a service container in a Windows job; introducing a self-hosted agent or external SQL Server would add infrastructure, credentials and maintenance for no benefit.
- The run-unique instance name plus per-run ownership token already prevents cross-run interference.
- No new secret or package is required (Windows integrated authentication).

## 4. Identity contract

`DVLD_TEST_EXPECTED_INSTANCE` must equal:

```
<SERVERPROPERTY('ServerName')>|<@@SERVICENAME>
```

The initialization script now queries this from the **running engine** (through the verified `master` probe) and publishes it as the pipeline variable `SqlExpectedInstance`. Both SQL test tasks receive it as `DVLD_TEST_EXPECTED_INSTANCE`. Validation remains exact (`OrdinalIgnoreCase`, trimmed) and unchanged.

## 5. Pipeline changes

1. `Initialize-TestSqlEngine.ps1` now:
   - queries the identity after the successful `master` probe;
   - rejects an empty or malformed identity;
   - publishes `##vso[task.setvariable variable=SqlExpectedInstance]…` alongside the existing variables.
2. New step **Validate SQL Test Environment Contract** (runs after initialization):
   - fails fast with one clear error if `SqlExpectedInstance` is empty/unexpanded/malformed;
   - prevents hundreds of misleading fixture-initialization failures.
3. `Run API Integration Tests` and `Run Infrastructure Integration Tests` now both receive:
   - `DVLD_TEST_CONNECTION`
   - `DVLD_TEST_EXPECTED_INSTANCE: $(SqlExpectedInstance)`
4. Cleanup step condition changed from `and(always(), SqlServerReady)` to **`always()`**. The cleanup script itself refuses to delete anything unless run ownership is positively verified, so this improves teardown on provisioning/test failure without weakening safety.
5. Added a `dotnet --info` diagnostic step to record the SDK actually used by restore.

No test filter, exit-code behaviour, result publishing, or coverage collection was changed.

## 6. Secrets

- No new secret was introduced. SQL access uses Windows integrated authentication on the ephemeral LocalDB instance.
- The existing `Jwt__SecretKey` variable-group secret is unchanged.
- No connection string, password, or token is printed by the new step; only the non-secret instance identity is logged.

## 7. Isolation and ownership

Unchanged:

- Databases are named `DVLD_IntegrationTests_<GUID>` / `DVLD_IntegrationTests_Api_<GUID>`.
- Each database carries a run-token extended property, `database_id` and `service_broker_guid`.
- Cleanup drops only databases whose ownership markers match; unknown/shared targets are skipped.
- `DVLDf`, `DVLD`, `master` as a test catalog, and unapproved databases remain rejected by `SqlServerTestEnvironment` and the initialization script.

## 8. Cleanup verification

Executed locally with the real scripts:

1. `Initialize-TestSqlEngine.ps1` on `DVLD_CI_CI_SQL_01_PROOF` → instance created, started, identity published.
2. SQL auth fixtures ran against it with the published `DVLD_TEST_EXPECTED_INSTANCE` → **10/10 passed**.
3. `Cleanup-TestSqlEngine.ps1` → 0 residual `DVLD_IntegrationTests_*` databases; instance stopped and deleted; `SqlLocalDB.exe info` shows only `MSSQLLocalDB`.

## 9. NuGet / SDK warning

The message “NET 5 has some compatibility issues with older NuGet versions (<= 5.7)”:

- No project targets `net5.0`; every project targets `net8.0` or `net8.0-windows`.
- `global.json` pins SDK `8.0.427`.
- The pipeline uses `UseDotNet@2` (global.json) and `DotNetCoreCLI@2` restore, i.e. the SDK-bundled NuGet.
- A local `dotnet restore` with SDK 8.0.427 produced no such warning.

Conclusion: **informational message from the hosted image tooling**, not a repository defect. The new `dotnet --info` step makes the effective SDK/NuGet version visible on future runs. No package or target framework was changed.

## 10. xUnit2013

`ProblemDetailsContractTests` line 104 used `Assert.NotEqual(0, errors.EnumerateObject().Count())`. Replaced with `Assert.NotEmpty(errors.EnumerateObject())` — semantically identical (at least one validation error), and the test passes (8/8 in the class).

## 11. Verification results

| Check | Result |
|---|---|
| Pipeline YAML parse | **Valid** (`js-yaml`; 2 stages, 22 backend steps) |
| Env wiring (parsed model) | API: connection + expected instance + JWT; Infrastructure: connection + expected instance |
| Init script end-to-end | **Passed** (instance created, identity `…LOCALDB#734FB90B…` published) |
| Fixture against provisioned instance with published identity | **10/10 SQL tests passed** |
| Cleanup | **Passed**; instance deleted; 0 residual databases |
| `Application.UnitTests` | **945 passed, 0 failed** |
| `API.IntegrationTests` database-free filter | **514 passed, 0 failed** |
| `ProblemDetailsContractTests` | **8 passed** |
| `dotnet restore` (SDK 8.0.427) | **0 errors, no NET 5 warning** |

## 12. Azure Pipelines runtime

**NOT YET VERIFIED.** The fix is committed; a pipeline run must still be triggered on Azure. Local execution proves the provisioning/identity/fixture path, but not the hosted agent environment. Expected outcome: `SqlExpectedInstance` is published, the validation step passes, and both SQL suites run against the isolated instance.

## 13. Remaining limitations

- Azure-hosted run pending (cannot be triggered from this environment).
- The validation step logs only that the contract is valid, not the identity value; the initialization step logs the identity for diagnostics.
- No SQL container or self-hosted agent was added (deliberate).

## 14. Safety confirmation

`Presentation (WPF)/` — untouched. `DVLDf` — never connected to; the test instance is ephemeral and deleted after verification. All existing SQL validation, ownership and cleanup safeguards remain intact.
