# DVLD — Comprehensive N+1 Query Audit and Remediation Assessment

**Status:** Static audit remains historical evidence. Follow-up implementation attempt on 2026-11-09 is **BLOCKED** before database execution: no verified isolated SQL Server endpoint was available. No source remediation or runtime closure is claimed.
**Repository state inspected:** branch `main`, HEAD `58c669fa84ca23402e960c25030fb23d727ac0ca`.
**Working tree at audit start:** clean (`git status --short --untracked-files=all` returned no entries).
**Baseline policy:** current local files are the source of truth; no older commit was used as the audit baseline.
**Database safety:** no database was connected, queried, or modified.

## 1. Executive summary

The prior license-list N+1 described as `PERF-01` is statically fixed in the current source: `LicenseQueryService.MapToDtosBatchAsync` makes batch calls outside the DTO loop, and `LicenseRepository.GetNewerLicensesBatchAsync` executes one EF query for any non-empty candidate batch. Empty batches return before querying. Existing integration tests cover functional batch behavior, but neither those tests nor this audit provide actual SQL-command-count measurements.

One separate linearly growing database-command path was found in no-show background processing. It first retrieves appointment IDs in a batch, then processes each appointment independently under a transaction and application lock. Each successful candidate currently causes one lock command, one locked appointment load, one test-existence query, and one update save, in addition to the batch selection query. This is **CONFIRMED N+1 command growth by static source trace**, but it is an intentional per-record consistency workflow and is not automatically a defect to replace with a bulk update. Its operational impact needs SQL profiling before deciding on a transaction-preserving optimization.

The current source also has an unrelated quadratic in-memory license-mapping pattern and multiple unpaged collection endpoints. These are recorded as other performance issues, not N+1 findings. No runtime timing, SQL query counts, plans, allocations, or database behavior were measured. The current implementation’s correctness under SQL Server load is therefore only partially verified.

## 2. Summary of findings

| ID | Domain | Finding | Classification | Severity | Evidence | Status |
|---|---|---|---|---|---|---|
| N1-01 | No-show background processing | Per-candidate transactional processing generates linearly growing SQL commands; includes a repeated test-existence query after loading the appointment with its `Test` navigation | CONFIRMED N+1 (static source trace; no runtime counter) | MEDIUM / P2 pending profiling | `NoShowProcessorService.cs:95-124,153-206`; `TestAppointmentRepository.cs:406-423,426-478,50-60`; `TestRepository.cs:48-55` | **PARTIALLY VERIFIED / BLOCKED**: follow-up authorized, but no safe SQL endpoint was established; optimization and runtime evidence remain unverified |
| N1-02 | License list DTO mapping | License lookup was batched; candidate count does not add one repository query per row | FIXED (static); query-count runtime verification pending | Historical P1, now source-fixed | `LicenseQueryService.cs:201-235`; `LicenseRepository.cs:420-479` | Source fix present; no current-run SQL measurement |
| PERF-01 | License DTO mapping | Nested scans over the result list can produce O(n²) in-memory comparisons, especially on the unpaged list path | OTHER PERFORMANCE ISSUE | MEDIUM | `LicenseQueryService.cs:221-229,258-264`; `LicensesController.cs:17-55` | Open; no code changed |
| PERF-02 | Collection endpoints | Several API list routes return unpaged collections, increasing materialization and response size as data grows | OTHER PERFORMANCE ISSUE | MEDIUM; profile and confirm product limits | Controller and repository list methods cited in §8 | Open; distinct from N+1 |
| PERF-03 | Dashboard aggregates | Eight scalar counts are combined into one SQL statement; this is not N+1, but large-table scan cost and index effectiveness need plans/profiling | NEEDS PROFILING | LOW / informational | `DashboardRepository.cs:13-54` | No runtime evidence |

## 3. Scope and methodology

### Inspected areas

- `Application/Services/` and `Application/Interfaces/` collection and workflow paths.
- `Infrastructure/Repositories/`, `Infrastructure/DVLDDbContext.cs`, and EF Core configuration inventory.
- API collection endpoints and `API/DVLD.Api/BackgroundServices/` loops.
- Domain navigation properties, Application mapping helpers, and DTO projections.
- Existing tests relevant to license batching, no-show processing, appointment stats, and SQL Server test setup.
- React source for apparent request fan-out inside collection rendering; no request-per-row pattern was identified in the source search. This was a focused scan, not a full frontend audit.
- Existing roadmap and execution-log records for the previously remediated `PERF-01`.

### Methods

Static repository searches were performed for iteration constructs, asynchronous query operators, service/repository calls, EF loading patterns, and mapper access. Candidate paths were traced through the service and repository implementation to the EF query or raw SQL command. Source-level command formulas below are explicitly marked as estimates. No test or benchmark was run, and no results are represented as runtime measurements.

### Exclusions and constraints

- No implementation or test files were changed. This assignment’s initial authorization is limited to audit/reporting.
- `Presentation (WPF)/` was excluded.
- No migration, integration test, benchmark, SQL connection, or database command was run. No `DVLDf` access occurred.
- The requested report did not exist before this audit; this file is newly created. Existing audit documents and the execution log were not modified.

## 4. Domain-by-domain audit inventory

| Domain | Source path traced | Static conclusion |
|---|---|---|
| People | `PeopleController` → `PersonService` → `PersonRepository` → `PersonConfiguration` | No per-person database call in mapper/list projection found. Search/list queries are composed before materialization; country is loaded by query include. Count + page retrieval is a fixed two-query pagination shape, not N+1. Some collection paths can be unpaged; see PERF-02. |
| Users | `UsersController` / `AuthController` → `UserService` / `AuthService` → `UserRepository` | No mapper-driven per-user query found. Profile/details use a query or projection. `IsUserActiveAsync` invoked by authentication validation is one request-level lookup, not one query per returned entity. The all-users endpoint is unpaged; see PERF-02. |
| Drivers | `DriversController` → `DriverService` → `DriverRepository` | Active-license counts are expressed inside the LINQ projection (`d.Licenses.Count(...)`) and are translated with the result query; the post-query loops only assign projected values. Not N+1. The controller also exposes an unpaged path. |
| Local driving applications | Controller → service → repository; `GetPassedTestCountsAsync` and `GetApplicationIdsWithLicensesAsync` | List path uses a bounded page query plus count. Summary enrichment is performed by batch/grouped repository queries before mapping, not per row. No N+1 found in inspected paths. |
| Licenses | `LicensesController` → `LicenseQueryService` → `LicenseRepository` and `DetainedLicenseRepository` | Historical N+1 is statically fixed (N1-02). Batch lookups are bounded per call for the paged path. An unpaged list and O(n²) in-memory mapping remain (PERF-01/02). |
| License classes | Controller → service → repository | Reference data retrieved as a collection in one repository query. No per-row database work found. |
| Tests | `TestsController` → `TestService` → `TestRepository` | List query includes related appointment/type and user data in the query. DTO mapping is synchronous property mapping; no follow-up query in a loop found. All-tests list is unpaged; see PERF-02. |
| Test types | Controller → service → repository | Collection is loaded once and mapped in memory. No N+1 found. |
| Test appointments | Controller → service/repository; `TestWorkflowService`; availability and statistics paths | Listing query loads reference navigations before mapping. Attempt stats include the related test and aggregate in memory after one query. Scheduling availability fetches the data set before iterating date/slot values. `AcquireSchedulingLocksAsync` runs exactly two application-lock commands per scheduling attempt, independent of collection size; NOT N+1. No-show batch processing is separately recorded as N1-01. The all-appointments route is unpaged; see PERF-02. |
| Detained licenses | Controller → service → repository | Related license/person/user/application data is loaded by includes in the collection query; no per-row mapper access found. All-detained list is unpaged; see PERF-02. |
| International licenses | Controller → service → repository | List query includes related application, driver/person, domestic license, and creator; mapping reads loaded navigations. No per-row query found. All-international list is unpaged; see PERF-02. |
| Application types and countries | Controller → service → repository | Reference lists use one collection query. No N+1 pattern found. |
| Dashboard and statistics | Controller → service → `DashboardRepository` | One SQL command with scalar subqueries for eight counts. The scalar subqueries execute within one command/round-trip; not N+1. Needs query-plan profiling at scale. |
| Authentication and authorization | `Program.cs` token validation → `IUserService` → `UserRepository` | Active-user validation is a single lookup per authenticated request. It does not scale with returned collection records and is NOT N+1. It can contribute a fixed DB cost per HTTP request; that is a separate profiling question. |
| Background services | License expiration and no-show hosted services | License expiration delegates to a set-based repository operation. No-show uses per-candidate lock/recheck/transaction processing and is N1-01. |
| Mappers / EF navigation | `Application/Mappings/`, `Application/Mappers/`, Domain entities, context registration | Mapper methods inspected are synchronous field/property transformations. Navigation properties are `virtual`, but lazy-loading proxies are not configured in the inspected API DbContext registration; no lazy-loading N+1 path was identified. |
| React request orchestration | `ClientApp/src` request/collection-render search | No request call was found directly inside `.map`/`forEach` rendering patterns in the focused scan. This does not establish that every page’s hook orchestration is free of repeated independent requests. |

## 5. Confirmed and potential N+1 findings

### N1-01 — No-show batch performs database work per candidate

- **Current follow-up disposition (2026-11-09): BLOCKED before runtime execution; N1-01 remains open.** The current environment has no `DVLD_TEST_CONNECTION`. `SqlLocalDB.exe info` failed while reading LocalDB instance registry configuration, and Windows service enumeration was denied by Service Control Manager permissions. No endpoint, account permissions, Extended Events capture, database test, or SQL command measurement could be verified. No database connection was attempted.
- **Additional fixture isolation concern:** `SqlServerTestDatabase` replaces `InitialCatalog` with a generated name but does not clear or reject `AttachDBFilename`; therefore arbitrary connection options cannot be ruled out from source inspection alone. Its setup/migration and teardown also require database creation/drop privileges, which cannot be confirmed without an approved endpoint and account. Do not use this fixture until these conditions are reviewed and isolation is verified.
- **Implementation disposition:** The explicit `IsTestAlreadyTakenAsync` call was retained. The loaded `Test` navigation alone has not been proven safe under an actual synchronized `TestService` versus NoShow SQL Server concurrency test. No source or test implementation was made.
- **Exact prerequisite to resume:** provide `DVLD_TEST_CONNECTION` for a designated isolated test SQL Server, with the host/instance and database scope approved and verified, a least-privilege test account able to create/migrate/drop only generated `DVLD_IntegrationTests_<GUID>` databases (or an approved equivalent setup), and no file-attach/redirect options. Then harden/verify fixture isolation before executing any test.

- **Classification:** CONFIRMED N+1 command growth by static source trace. Runtime SQL-command counts and elapsed time were not captured.
- **Severity:** MEDIUM / P2 pending profiling. Defaults limit the processor to batches of 50, at most 20 batches per invocation, with a 60-second polling interval. This caps one run at 1,000 candidate IDs but can still produce substantial sequential DB traffic when many appointments qualify.
- **Execution path:** `NoShowBackgroundService.ExecuteAsync` → `NoShowProcessorService.ProcessPendingNoShowsAsync` → `ProcessBatchAsync` → per-ID `ProcessSingleAppointmentAsync` → repository commands and Unit of Work transaction.
- **Evidence:**
  - `Application/Services/NoShowProcessorService.cs:95-124` retrieves one candidate-ID batch, then loops and awaits `ProcessSingleAppointmentAsync` for each ID.
  - `Application/Services/NoShowProcessorService.cs:162-206` starts a separate transaction per candidate, calls `AcquireAppointmentLockAsync`, `GetForUpdateAsync`, `IsTestAlreadyTakenAsync`, and saves eligible changes.
  - `Infrastructure/Repositories/TestAppointmentRepository.cs:414-423` retrieves IDs with one `ToListAsync` command.
  - `Infrastructure/Repositories/TestAppointmentRepository.cs:444-475` executes `sp_getapplock` once per candidate.
  - `Infrastructure/Repositories/TestAppointmentRepository.cs:426-478` implements that per-candidate lock command; `GetForUpdateAsync` at lines 50-60 is a separate EF query including `Test`.
  - `Infrastructure/Repositories/TestRepository.cs:48-55` executes a separate `AnyAsync` query for the test existence check.
  - On a successful candidate, `SaveChangesAsync` writes the no-show status and lock fields.
- **Static command model:** For a run that processes N eligible records successfully, source indicates about `4N + S` SQL commands, where `S` is the number of candidate-selection queries (including a final empty selection when a full batch is followed by no remaining candidates). This excludes transaction begin/commit operations and does not account for database/provider interception details.

| Eligible records | Source-derived expected DB commands | Measurement status |
|---:|---:|---|
| 1 | 5 (`1 + 4×1`) | Static estimate only |
| 10 | 41 (`1 + 4×10`) | Static estimate only |
| 50 | 202 (`2 + 4×50`, final empty fetch after a full batch) | Static estimate only |
| 100 | 403 (`3 + 4×100`, final empty fetch after two full batches) | Static estimate only |

- **Business/transaction impact:** The lock and per-item transaction protect against duplicate processing and state changes racing with test recording. Replacing this flow with a bulk update or parallel per-item queries without preserving the application lock, row revalidation, cancellation, and per-record outcomes could change correctness. No such change is recommended without a design and integration proof.
- **Potential reduction to investigate:** `GetForUpdateAsync` includes the appointment’s `Test` navigation, then the processor separately calls `IsTestAlreadyTakenAsync`. This appears to repeat an existence read for each candidate. Verify loaded-navigation consistency and concurrent test-creation semantics before removing that query; the same explicit existence check also appears in single-record `TestService.AddAsync` and may be required by its serializable transaction.
- **Recommended next step:** After explicit implementation authorization, add an SQL command interceptor to isolated integration tests and measure a successful batch at 1/10/50/100. Then evaluate a narrowly scoped reduction that preserves locking and transactional behavior. Do not parallelize the candidates or bypass the Unit of Work.
- **Regression tests:** Existing `AppointmentNoShowIntegrationTests` cover lifecycle, idempotency, and concurrent workers, but no SQL-command budget. Add actual command-count coverage only after the human authorizes implementation/test changes and a test-only SQL connection is verified.

### N1-02 — LicenseQueryService previous per-license lookup is batched

- **Classification:** FIXED (static source); query-count runtime proof remains pending.
- **Historical finding:** `PERF-01` previously described a sequential `GetNewerLicenseForDriverAndClassAsync` call per mapped row. The current method body no longer makes that call inside the mapping loop.
- **Current implementation evidence:**
  - `Application/Services/LicenseQueryService.cs:209-215` gathers distinct IDs and calls `GetActiveDetainedLicenseIdsAsync` once before mapping.
  - `Application/Services/LicenseQueryService.cs:219-235` identifies missing in-page candidates and, when non-empty, calls `GetNewerLicensesBatchAsync` once before the DTO loop.
  - `Application/Services/LicenseQueryService.cs:240-285` maps rows in memory and uses a dictionary lookup for external newer-license results.
  - `Infrastructure/Repositories/DetainedLicenseRepository.cs:54-76` returns early for empty input; otherwise it performs one `ToListAsync` with an `IN` predicate.
  - `Infrastructure/Repositories/LicenseRepository.cs:420-479` returns early for null/empty valid candidates and performs one `ToListAsync` query for non-empty candidate pairs; matching then occurs in memory.
- **Batch edge cases:**
  - Duplicate driver/class candidates are grouped by pair at `LicenseRepository.cs:434-440`; each candidate is checked against the same pair’s newer licenses.
  - The result dictionary is keyed by candidate LicenseID (`:467-475`), so repeated identical IDs overwrite the same mapped newest record. No incorrect cross-driver/class matching appears in the inspected code.
  - Newer records are selected by descending LicenseID (`:460-465`), consistent with the repository’s newer-license comparison by ID. Whether LicenseID is the intended business ordering is a domain invariant to confirm; current code and tests use that ordering.
  - Empty candidate input returns an empty dictionary before database access (`:424-432`).
  - Paged list size is capped at 100 by `PaginationRequest`; candidate batches in that path are therefore at most 100 records. Other callers should still preserve bounded inputs.
  - For a non-empty batch of 1, 10, 50, or 100 candidates, the repository source contains one `ToListAsync` call. This is a static query-shape conclusion, not a measured SQL count.
- **Correctness/tests:** Existing `LicenseRepositoryTests.cs:2033-2133` cover null/empty input, multiple drivers/classes, newest selection, unrelated pairs, and cancellation. Application unit tests verify the batch service call. No test in the inspected source asserts a bounded SQL command count with an interceptor.

## 6. Other performance findings (not N+1)

### PERF-01 — Quadratic license DTO matching

`LicenseQueryService.MapToDtosBatchAsync` repeatedly scans the license list:

- `Application/Services/LicenseQueryService.cs:221-229` runs `licenses.Any(...)` while selecting external lookup candidates.
- `Application/Services/LicenseQueryService.cs:258-264` filters and sorts the full list again for each eligible license to find a newer in-page license.

This is in-memory O(n²) work (with ordering overhead in the second scan), not additional database round-trips. Paged requests are capped at 100, but the legacy unpaged license route calls the same mapper and can supply an unbounded list. A precomputed maximum/newer-license lookup grouped by `(DriverID, LicenseClass)` could reduce CPU work without changing query behavior; implementation was not authorized.

### PERF-02 — Unpaged collection retrieval

The following current API paths return full collections without pagination in the controller/service/repository path inspected:

- Licenses: `LicensesController.cs:17-55`; `GetAllAsync()` routes to `LicenseQueryService.GetAllAsync()` and `LicenseRepository.GetAllLicensesAsync()`.
- People: `PeopleController.cs:16-57` retains an unpaged list path when pagination parameters are absent.
- Drivers: `DriversController.cs:20-58` retains an unpaged service call when pagination/query parameters are absent.
- Users: `UsersController.cs:19-28` calls `GetAllUsersAsync()`.
- Test appointments: `TestAppointmentsController.cs:16-24` calls repository `GetAllAsync()`.
- Tests: `TestsController.cs:17-25` calls `GetAllAsync()`.
- Detained licenses: `DetainedLicensesController.cs:16-22` calls `GetAllAsync()`.
- International licenses: `InternationalLicensesController.cs:17-23` calls `GetAllAsync()`.
- Applications: `ApplicationsController.cs:16-24` calls `GetAllApplicationsAsync()`.

These paths can increase database materialization, memory use, and response size as data grows; none is an N+1 finding by itself. Confirm external callers and API compatibility before changing them. Pagination changes may require approval under API/contract governance.

### PERF-03 — Dashboard SQL plan needs profiling

`DashboardRepository.GetStatisticsAsync` issues one parameterized SQL command with eight scalar `COUNT(*)` subqueries (`Infrastructure/Repositories/DashboardRepository.cs:13-54`). This is one round-trip, not eight ORM queries or N+1. Its cost depends on table sizes, indexes, and plan choices; capture an execution plan and duration in a safe test environment before considering redesign.

## 7. Not-N+1 patterns and rejected false positives

| Pattern | Why it is not classified as N+1 |
|---|---|
| `DriverRepository.GetAllAsync` / paged path (`:60-77,96-204`) | Active license count is projected into the same LINQ query; the loop only assigns the already projected count. |
| `TestAppointmentRepository.GetAttemptStatsAsync` (`:481-533`) | One query includes each appointment’s `Test`; the result loop counts in-memory values. |
| `TestWorkflowService.GetNextTestTypeAsync` (`:49-83`) | A fixed number of repository calls (status and passed type IDs) precede iteration over a static three-item test-type list. |
| `LocalDrivingLicenseApplicationService.CancelAsync` (`:484-505`) | Retrieves active appointments once, mutates the tracked collection in memory, and saves once; no database call occurs inside the loop. |
| `TestAppointmentRepository.AcquireSchedulingLocksAsync` (`:198-255`) | Executes two app-lock commands for two fixed lock resources per scheduling attempt, independent of the number of returned entities. |
| `TestAppointmentService.GetAvailabilityAsync` (`:645` onward) | Retrieves availability data before looping over dates and known time slots; those loops are in-memory. |
| `LicenseQueryService.MapToDtosBatchAsync` (`:240-285`) | The DTO loop has no awaited repository call. It has a quadratic CPU pattern (PERF-01), not database N+1. |
| `DashboardRepository` scalar subqueries | One SQL command with multiple database-side scalar expressions is not multiple per-record round-trips. |
| `AsSplitQuery` on license reads | Current license includes are reference navigations, not per-row lazy loads. A fixed split-query shape, if emitted, is not automatically N+1; confirm SQL commands if query shape changes. |

## 8. Previously optimized LicenseQueryService validation

| Requirement | Static result |
|---|---|
| Batch query bounded for ordinary paged inputs | One `ToListAsync` in `GetNewerLicensesBatchAsync`; `PageSize` clamps at 100. Runtime not measured. |
| Duplicate driver/class combinations | Grouped before predicate construction; candidates still resolved by their own license IDs. Functional integration tests cover multiple candidates per pair. |
| Newest license selection | In-memory descending LicenseID selection. Functional integration test covers three licenses. |
| Empty input | Returns an empty dictionary before querying. Functional integration test present. |
| Renewal eligibility across paths | Paged and application-specific lists request external newer lookup when page/selection may omit newer records; full driver/class/person/unpaged lists compare their returned set in memory. Single-license details use bounded repository checks. No static inconsistency found in inspected paths. |
| Memory behavior | The batch query can return all newer licenses for each candidate pair above the minimum candidate ID, then scans those rows per candidate. It is bounded by candidate pair count on the paged path but may over-fetch license history; profile and consider grouping results in memory if it is material. |
| Query-count regression test | Not found. Existing repository tests verify results but do not assert actual SQL command counts. |

### Documentation discrepancy

The current source contains the batch implementation and the execution log says `PERF-01` was completed. However, `ENGINEERING-ROADMAP.md:99-110` still says `PERF-01` is `Confirmed / Planned` and `NOT AUTHORIZED`. Treat the roadmap status as stale relative to the current source and execution-log entries; this audit did not edit either document or decide the roadmap lifecycle. The execution log’s historical test claims were not rerun in this audit.

## 9. Runtime evidence, tests, and benchmarks

| Evidence requested | Result |
|---|---|
| SQL commands for 1/10/50/100 records | No SQL command interceptor is present for N+1 regression tests. N1-01 counts in §5 are source-derived estimates only. License batch counts are static query-shape conclusions only. |
| Execution time / allocations / rows / SQL plans | Not measured. |
| Integration test database | Not opened. The repository fixture requires `DVLD_TEST_CONNECTION` and creates a generated `DVLD_IntegrationTests_<GUID>` database, but this audit did not verify an isolated connection and did not execute it. `DVLDf` was not accessed. |
| Existing no-show coverage | `AppointmentNoShowIntegrationTests.cs` covers functional outcomes, boundaries, idempotency, and concurrent workers. It does not assert SQL command count. Tests were not run in this audit. |
| Existing license batch coverage | `LicenseRepositoryTests.cs:2033-2133` covers batch output/empty/cancellation behavior but does not assert query count. Tests were not run in this audit. |
| Build / unit / API / infrastructure tests | Not run; no implementation changes were authorized or made. |

## 10. Remediation plan (not implemented)

1. **N1-01, authorize after review:** Add SQL command-count instrumentation to the existing isolated Infrastructure integration-test fixture; verify candidate sizes and preserve the existing no-show concurrency/idempotency scenarios. Do not run unless an isolated test connection is verified. Establish measured baseline before code changes.
2. **N1-01, design review:** Determine whether the explicit `IsTestAlreadyTakenAsync` query can safely be replaced by the already-loaded `appointment.Test` state under the same lock/transaction guarantees. This affects transactional behavior and requires proving that test creation cannot race past the lock. Do not remove it based only on the duplicate-looking include.
3. **N1-01, only if profiling warrants:** Evaluate a conditional/set-based path only if it preserves application lock semantics, per-item result reporting, cancellation, state revalidation, and atomicity. No batching or parallelization change is recommended before this proof.
4. **PERF-01:** Precompute a grouped in-memory newer-license lookup once per result set to avoid repeated scans; preserve existing LicenseID ordering and renewal reasons. Add tests for duplicates, missing relationships, page boundaries, and large unpaged result behavior if that legacy path remains supported.
5. **PERF-02:** Inventory callers and decide pagination contracts for unpaged endpoints. This may require separate API approval and is not a narrow N+1 fix.
6. **PERF-03:** Capture dashboard query plans and table/index statistics in an isolated environment; make no index/schema changes without the applicable approval.
7. **Governance:** Reconcile `PERF-01` status between `ENGINEERING-ROADMAP.md` and `docs/IMPLEMENTATION-EXECUTION-LOG.md` through the normal authorized roadmap process. This audit did not update the execution log.

## 11. Files changed

- Original audit added this document. Follow-up on 2026-11-09 updated only the N1-01 disposition/evidence above.
- No application, infrastructure, API, domain, contracts, test, frontend, WPF, configuration, or database files were changed.
- No source fix or regression test was added because safe SQL Server isolation could not be established; runtime test execution remains blocked.

## 12. Remaining risks and limitations

- N1-01 command counts are inferred from the code path, not captured from an EF `DbCommandInterceptor` or SQL trace.
- The test fixture and SQL Server provider can alter observed command totals; transaction-control events may not appear as `DbCommand` events.
- Production dataset size, no-show backlog frequency, command duration, lock wait, deadlock rate, and query plans are unknown.
- Existing tests and build were not executed during this audit.
- Other collection retrieval paths and quadratic DTO mapping deserve performance follow-up but are not evidence of N+1 by themselves.
- The documented `PERF-01` roadmap state conflicts with current source and the execution log.

## 13. Final status

**Audit/report: complete for the statically inspected active backend paths.**
**N+1 remediation: not performed; explicit implementation authorization is required.**
**Runtime query-count verification: pending an approved run against a verified isolated test database.**
**No database was accessed, and no changes were made outside this newly created audit report.**
