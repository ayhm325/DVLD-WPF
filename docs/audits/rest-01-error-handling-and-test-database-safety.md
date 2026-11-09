# REST-01 — Error Handling Standardization & Test Database Safety Gate

> **Task:** REST-01 (RFC 7807 ProblemDetails) plus the mandatory test-host database safety prerequisite.
> **Date:** 2026-11-09
> **Repository:** `D:\DVLD-WPF`
> **Baseline commit supplied by the task:** `d52098488eb25cbf5888b58fadb394c0028d9d27`
> **Actual `HEAD` at execution:** `5ce662155a448d527ef32f5211ecb322bc1a2fb6` (`fix(test): harden SQL integration database lifecycle cleanup`)
> **WPF:** untouched. **Schema/migrations:** none. **`DVLDf`:** never accessed.

---

## 1. Baseline and authorization notes

1. `d520984` is an ancestor of `HEAD` (`git merge-base --is-ancestor` exited 0). The repository had advanced one commit before this task started, and that commit is itself a test-database lifecycle hardening change, directly relevant to Part A.
2. The working tree contained **pre-existing uncommitted user work** in the test infrastructure:
   - Modified: `Test/API.IntegrationTests/Infrastructure/SqlServerApiWebApplicationFactory.cs`, `Test/Infrastructure.IntegrationTests/Fixtures/SqlServerTestDatabase.cs`, `Test/Shared/SqlServerTestEnvironment.cs`
   - Untracked: `SqlServerApiFactoryDisposal.cs`, `SqlServerApiFactoryDisposalTests.cs`, `SqlServerApiFactoryInitializationException.cs`, `SqlServerApiFactoryLeaseCleanupTests.cs`
   - Untracked: `docs/audits/POST-COMMIT-SECURITY-TEST-INFRASTRUCTURE-REVIEW-5ce6621.md`
3. That work was **preserved untouched** and excluded from the commit. The REST-01 implementation deliberately avoids modifying those files, so the commit remains a coherent, self-contained change set on top of `HEAD`.

---

## 2. Part A — Test-host exposure to `DVLDf`

### 2.1 The vulnerability had two independent causes

| # | Cause | Evidence |
|---|---|---|
| A1 | `Program.cs` performed a **database write during host construction**, before `app.Run()`. `WebApplicationFactory` executes the entry point up to `app.Run()`, so every controller-test host ran `DeactivateExpiredLicensesAsync`. | Former block in `API/DVLD.Api/Program.cs` (removed in this change) |
| A2 | The **mock API test host inherited the production connection string**. `ApiWebApplicationFactory` replaced application services but never overrode `ConnectionStrings:DVLDConnection`, which resolved to `Server=.;Database=DVLDf` from `API/DVLD.Api/appsettings.json`. | `Test/API.IntegrationTests/Infrastructure/ApiWebApplicationFactory.cs` (before), `API/DVLD.Api/appsettings.json` |

Either cause alone was survivable; together they allowed `ApiSmokeTests` and every controller test class to execute:

```sql
UPDATE Licenses SET IsActive = 0 WHERE ExpirationDate < <today>;
```

against `DVLDf` whenever a default SQL Server instance exposing that database was reachable (typically a developer workstation). The startup exception handler swallowed failures, so the condition was invisible.

### 2.2 Remediation

1. **Removed the startup sweep entirely.** `LicenseExpirationBackgroundService.ExecuteAsync` already performs its first sweep immediately (before its first `Task.Delay`) and then on `LicenseExpirationOptions.PollingIntervalSeconds` (default 60s, `IsEnabled` default `true`). Production behavior is therefore preserved without a write in the host-construction path. The removal is documented in `Program.cs`.
2. **Added a pure fail-fast guard:** `Infrastructure/Configuration/SqlServerConnectionSafety.EnsureSafeForHost`. It parses the resolved connection string and, when the host is a test host, rejects:
   - `DVLD`
   - `DVLDf`
   - every catalog that is not `DVLD_IntegrationTests_<32 hex GUID>` or `DVLD_IntegrationTests_Api_<32 hex GUID>`
   - `AttachDBFilename`, user instances, and failover partners

   It never opens a connection, so rejecting a protected target cannot touch it. Non-test hosts (Production/Development) are unaffected and may legitimately target `DVLDf`.
3. **Test-host identification (two independent signals):**
   - environment name `Testing` (`ApiWebApplicationFactory`, plus the safety test's unsafe-host factory)
   - process switch `DVLD_TEST_HOST=1`, set by a `[ModuleInitializer]` in the API test assembly, so every host created by that assembly is validated — including factories that do not set `Testing`.
4. **Non-connecting substitute for the mock host:** `ApiWebApplicationFactory` now pins `Server=(localdb)\DVLD_TEST_HOST_NO_DATABASE;Initial Catalog=DVLD_IntegrationTests_Api_<GUID>;Connect Timeout=1`. That LocalDB instance is never created, so any accidental unmocked dependency fails immediately instead of reaching an application database.
5. **Observable safety:** `ApiWebApplicationFactory` replaces `DbContextOptions<DVLDDbContext>` with an instrumented registration that installs `ConnectionAttemptRecorder` (`DbConnectionInterceptor`). Tests assert the count remains **0**.
6. **Test-only JWT signing key:** the mock host supplies its own signing key, removing the suite's dependency on a developer/CI `Jwt__SecretKey` environment variable.

### 2.3 Cleanup ownership (verified, not modified)

The pre-existing cleanup design (commit `5ce6621` plus uncommitted WIP) already enforces ownership: generated `DVLD_IntegrationTests_<GUID>` names, a `DVLD_Test_Run_Token` extended property, `database_id` + `service_broker_guid` identity match, and an outcome enum (`Dropped`/`AlreadyAbsent`/`OwnershipMismatch`) that retains the lease on any doubt. This task added tests against the pure validators but did **not** modify cleanup code, to avoid entangling the user's in-flight work.

### 2.4 Evidence

| Assertion | Test | Result |
|---|---|---|
| Guard rejects `DVLDf` and `DVLD` | `TestHostDatabaseSafetyTests.Guard_Rejects_ProtectedApplicationDatabases` | Pass |
| Guard rejects unapproved catalogs (`master`, prefix-only, non-GUID, arbitrary) | `Guard_Rejects_UnapprovedTestDatabases` | Pass |
| Guard accepts a generated isolated catalog | `Guard_Accepts_GeneratedIsolatedTestDatabase` | Pass |
| Guard is inert for non-test hosts (production target preserved) | `Guard_IsInert_ForNonTestHosts` | Pass |
| A test host configured with `DVLDf` fails fast, naming the protected database | `TestHost_ConfiguredWithProtectedDatabase_FailsFast` | Pass |
| Mock host starts and serves a request with **zero** connection attempts | `MockTestHost_StartsAndServesRequests_WithoutOpeningAnyConnection` | Pass |
| Resolved `DbContext` catalog is an isolated test database | `MockTestHost_DbContextTargetsIsolatedTestDatabase_NotApplicationDatabase` | Pass |
| Cleanup naming rejects `DVLD`/`DVLDf` and non-generated suffixes | `Cleanup_Rejects_*` | Pass |
| Test connection configuration rejects `DVLDf` and missing configuration | `TestConnectionConfiguration_Rejects_*` | Pass |

The "fails fast" test proves the guard fires **before** any `DbContext` is used; even if the guard were removed, the host no longer performs database work during construction.

---

## 3. Part B — Error catalog

### 3.1 Measurement

The prior architecture report's "143 `Result.Failure` call sites" was a combined regular expression over every failure factory. Measured values in `Application/Services`:

| Factory | Count |
|---|---:|
| `Result.Failure(...)` (direct, untyped) | **26** |
| `.FromFailure(...)` (all forms, post-REST-01) | **42** (36 typed `Result<T>.FromFailure`, 6 non-generic `Result.FromFailure`) |
| `Result.ValidationFailure(...)` / `FromValidationFailure(...)` | many (input validation) |
| `Result.NotFound(...)` / `FromNotFound(...)` | many (missing resources) |
| `Result.Conflict(...)` / `FromConflict(...)` | many (business state) |
| `Result.Forbidden(...)` / `FromForbidden(...)` | 16 (defensive authentication/authorization guards) |

### 3.2 Direct `Result.Failure` sites by service

| Service | Count | Verdict |
|---|---:|---|
| `LocalDrivingLicenseApplicationService` | 5 | Persistence/consistency + missing parent aggregate → 500 |
| `PersonService` | 4 | Persistence failures → 500 |
| `UserService` | 3 | Persistence failures → 500 |
| `TestAppointmentService` | 3 | Persistence failures → 500 |
| `DetainedLicenseService` | 3 | Persistence failures → 500 |
| `ApplicationTypeService` | 2 | Persistence failures → 500 |
| `DriverService` | 2 | Persistence failures → 500 |
| `LicenseClassService` | 2 | Persistence/configuration failures → 500 |
| `ApplicationService` | 1 | Persistence failure → 500 |
| `TestTypeService` | 1 | Persistence failure → 500 |
| **Total** | **26** | **All are genuine server-side conditions; 500 is correct.** |

The audit therefore contradicts the assumption that `Result.Failure` is systematically misused as a 500: the direct sites are dominated by "zero rows affected", "repository update returned false", and "parent aggregate missing" — conditions a client cannot fix.

### 3.3 What was actually misclassified

| Condition | Symptom before | Correct classification |
|---|---|---|
| Invalid credentials (`AuthService.LoginAsync`) | `Result.Failure` + controller special case returning `{ error }` | **401** `ProblemDetails` via `ErrorType.Unauthorized` |
| Missing `file` on photo upload | `{ error = "..." }` anonymous envelope | **400** `ValidationProblemDetails` with `errors.file` |
| Malformed JSON body | unhandled `BadHttpRequestException` → **500** | **400** `ValidationProblemDetails` |
| 401/403 produced by middleware | empty body | **401/403** `ProblemDetails` |
| Unmatched route / wrong method | empty 404/405 | **404/405** `ProblemDetails` |
| Rate-limit rejection | empty 429 | **429** `ProblemDetails` |
| Client disconnect | logged as unhandled / generic 500 path | **499**, informational log, no body |

### 3.4 Catalog of expected failure conditions

| Category | Representative condition | Origin | Client message | HTTP | Logged |
|---|---|---|---|---|---|
| Validation | Invalid ID, malformed field, invalid enum | `*Validator` → `Result.ValidationFailure` | validator text | 400 | No |
| Validation | Missing/invalid body or model binding | `ApiBehaviorOptions.InvalidModelStateResponseFactory` | field errors | 400 | No |
| Authentication | Invalid username or password | `AuthService.LoginAsync` | "Invalid username or password." | 401 | No (rate-limited; audit logging deferred) |
| Authentication | Missing/expired token | JWT challenge → status-code pages | generic | 401 | No |
| Authorization | Authenticated caller lacking role/permission | `[Authorize(Policy=...)]`, service guards | curated / generic | 403 | No |
| Not found | Requested aggregate does not exist | Services | curated | 404 | No |
| Conflict | Duplicate username / person-user link | `UserService`, `UnitOfWork` constraint translation | curated | 409 | No |
| Conflict | Illegal state transition (completed, cancelled, locked, expired, already issued) | License/test/appointment services | curated | 409 | No |
| Conflict | Scheduling lock not acquired | `TestAppointmentService` | retry guidance | 409 | No |
| Infrastructure/consistency | `SaveChanges` returned 0, repository update false, missing parent row | 26 direct `Result.Failure` sites | generic | 500 | **Yes (error)** |
| Unexpected exception | Any unhandled exception | `GlobalExceptionHandler` | generic | 500 | **Yes (error)** |
| Malformed request | Invalid JSON / unreadable body | `GlobalExceptionHandler`, MVC | generic/field | 400 | Yes (warning) |
| Cancellation | Client aborted request | `GlobalExceptionHandler` | none | 499 | Yes (information) |

### 3.5 HTTP mapping rules

| `ErrorType` | Status | Payload | Detail policy |
|---|---:|---|---|
| `Validation` | 400 | `ValidationProblemDetails` (`errors["request"]`) | Aggregated message is safe; structured messages split per line |
| `Unauthorized` | 401 | `ProblemDetails` | Curated message |
| `Forbidden` | 403 | `ProblemDetails` | Curated message |
| `NotFound` | 404 | `ProblemDetails` | Curated message |
| `Conflict` | 409 | `ProblemDetails` | Curated message |
| `Failure` / undefined | 500 | `ProblemDetails` | **Generic only**; internal text logged, never sent |
| Success passed to failure mapper | 500 | `ProblemDetails` | Generic; programming error logged; never throws |

No failed operation returns 200. A successful `Result` cannot be converted into a failure response without a logged programming error, and the mapper no longer throws when a controller omits an error guard.

---

## 4. ProblemDetails architecture

| Concern | Implementation |
|---|---|
| Single payload factory | `API/DVLD.Api/Results/ApiProblemDetails.cs` (`status`, `title`, `detail`, `type`, `instance`, `traceId`, `application/problem+json`) |
| Stable type URIs | `ProblemTypes` (`urn:dvld:error:validation|unauthorized|forbidden|not-found|conflict|internal|...`) |
| Result translation | `API/DVLD.Api/Results/ResultHttpMapper.cs` — the only `ErrorType` → HTTP mapping |
| Exceptions | `API/DVLD.Api/Security/GlobalExceptionHandler.cs` — cancellation 499, `BadHttpRequestException`/`JsonException` 400, duplicate conflicts 409, everything else generic 500 |
| Model binding | `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Program.cs` |
| Middleware status codes | `UseStatusCodePages` writer in `Program.cs` (401, 403, 404, 405, 429) |
| Content type | Every failure sets `application/problem+json` explicitly |
| Trace correlation | Per-request `traceId` (`HttpContext.TraceIdentifier`) on every payload; verified unique per request |
| Leak prevention | 500 details are constants; exception messages, SQL text, and connection strings are never serialized |

---

## 5. React compatibility

`ClientApp/src/services/apiClient.ts` now exposes:

- `ApiError { status, title, detail, fieldErrors, traceId }`
- `UnauthorizedError extends ApiError` (401)
- `ForbiddenError extends ApiError` (403 — existing name preserved)
- `ApiFieldErrors = Record<string, string[]>`

Parsing order: `detail` → flattened `errors` → legacy `error`/`message` → `title` → HTTP fallback. This keeps backward compatibility with the previous `{ error }` shape while giving ProblemDetails-first behavior. 401 still triggers the configured unauthorized handler. No UI component was redesigned.

---

## 6. Security protections added

1. Test hosts cannot reach `DVLD`, `DVLDf`, or any non-isolated catalog (fail-fast, connection-free guard).
2. Startup no longer performs any database write.
3. 500 responses never include exception text, SQL messages, connection strings, or stack traces (asserted end-to-end).
4. Duplicate-constraint exceptions are mapped to curated 409 messages rather than leaking provider text.
5. Client cancellation is no longer reported as a server error.
6. `Result` failures are logged server-side without exposing their text to clients.

---

## 7. Test evidence

| Suite | Scope | Result |
|---|---|---|
| `Test/Application.UnitTests` | All unit tests (including updated `AuthServiceTests`) | **935 passed, 0 failed, 0 skipped** |
| `Test/API.IntegrationTests` (database-free filter) | 20 controller classes + 1 background-service class + smoke + JWT clock + 4 new suites | **505 passed, 0 failed, 0 skipped** |
| New mapper suite | `ResultHttpMapperTests` | 10 passed |
| New exception suite | `GlobalExceptionHandlerTests` | 6 passed |
| New contract suite | `ProblemDetailsContractTests` | 8 passed |
| New safety suite | `TestHostDatabaseSafetyTests` | 17 passed (incl. theories) |
| Frontend | `npm run lint` | exit 0 |
| Frontend | `npm run build` (`tsc -b && vite build`, temp outDir) | exit 0 |
| Backend build | `DVLD.Api` + all test projects | 0 errors (NU1900 offline vulnerability-feed warnings only) |
| Post-commit focused API | `GlobalExceptionHandlerTests` + `ResultHttpMapperTests` + `ProblemDetailsContractTests` | **26 passed, 0 failed** |
| Post-commit focused unit | All service test classes asserting `ErrorType.Failure` + `AuthServiceTests` + cancellation tests | **676 passed, 0 failed** |

### Blocked (not executed) — SQL-dependent and therefore not safely runnable

`DVLD_TEST_CONNECTION` and `DVLD_TEST_EXPECTED_INSTANCE` are unset in this environment. The fixture fails fast by design. Not executed:

- `Test/Infrastructure.IntegrationTests` — 26 files, static inventory **656** `[Fact]`/`[InlineData]` cases (previously recorded as 657).
- `Test/API.IntegrationTests` SQL suites — 12 classes: `AppointmentAvailabilityWorkflowTests`, `DetainedLicenseWorkflowTests`, `InternationalLicenseWorkflowTests`, `LicenseIssuanceWorkflowTests`, `LicenseRenewalWorkflowTests`, `LicenseReplacementWorkflowTests`, `LocalDrivingLicenseApplicationWorkflowTests`, `ReleaseDetainedLicenseWorkflowTests`, `TestAppointmentWorkflowTests`, `TestResultWorkflowTests`, `SqlServerApiFactoryDisposalTests`, `SqlServerApiFactoryLeaseCleanupTests` — static inventory **96** `[Fact]`/`[InlineData]` cases.

No attempt was made to provision a database or to point tests at `DVLDf`.

---

## 8. Deferred findings (recorded, not implemented)

See `ENGINEERING-ROADMAP.md` §7 (`CAND-01`…`CAND-11`) for the full list with severity and dependencies. Highest priority:

- `CAND-01` JWT role/password staleness (P0 security).
- `CAND-02` last-administrator check-then-act race (P0 availability/security).
- `CAND-03` misleading `Get*ForUpdateAsync` reads (P1 integrity).
- `CAND-08` design-time factory hardcodes `DVLDf` (P1 data safety).

---

## 9. Residual risks and honest limitations

1. **SQL integration suites were not executed.** Error-mapping changes were verified against 505 database-free API tests and 935 unit tests, but the SQL workflow suites remain unverified in this environment.
2. **`SqlServerApiWebApplicationFactory` was not modified** because it carries uncommitted user work. It remains protected by construction (generated isolated catalog, ownership-verified cleanup) and by the `DVLD_TEST_HOST` guard, but the guard's behavior for that factory was not exercised end-to-end here.
3. **Defensive authentication guards in application services still return `Forbidden`** ("Authenticated user is required"). These paths are unreachable through HTTP because every controller is protected by `[Authorize]`; changing them to 401 would churn 16 service tests without changing observable API behavior. They are documented as defensive and can be revisited with `CAND-01`.
4. **No deployment-time verification** was performed; build and tests only.

---

## 10. Post-commit focused verification (commit `a200062`, reviewed as `HEAD~1`)

### 10.1 Cancellation and the 499 decision

**Decision: retain 499.**

Rationale, based on the actual execution path:

- The 499 branch in `GlobalExceptionHandler.TryHandleAsync` only sets `Response.StatusCode` when the response has **not started**, and it writes **no body**. The status therefore cannot reach the disconnected client and cannot alter the API contract.
- Its value is server-side observability: Kestrel/hosting and APM logs record 499, which separates client aborts from genuine 5xx responses and keeps error budgets and alerting accurate.
- Replacing it with 500 would pollute server-error metrics; 408 would misstate a client-side close as a request timeout; 503 would misstate dependency unavailability; returning 200/204 would falsely report success. `Status499ClientClosedRequest` is a stable ASP.NET Core constant and the de-facto nginx convention.

**Gap found and corrected:** for a **non-cancellation** exception that occurs after the client has already aborted, the handler fell through to `WriteProblemDetailsAsync` and attempted to write to the disconnected response. The write was caught (`OperationCanceledException`/`IOException`), but the attempt itself violated the "never write to a disconnected client" rule and could surface framework-level noise. `GlobalExceptionHandler` now checks `RequestAborted.IsCancellationRequested` after the `HasStarted` check, logs the underlying exception at **Warning**, and returns without writing.

**Exact behavior matrix (post-fix):**

| Situation | Handler result | HTTP effect |
|---|---|---|
| `OperationCanceledException` + `RequestAborted` cancelled | handled, `Information` log | status 499 if response not started; **no body** |
| Non-cancellation exception + `RequestAborted` cancelled | handled, `Warning` log with exception | **no write attempt**, no body |
| `OperationCanceledException` with `RequestAborted` **not** cancelled (application timeout token) | handled, `Error` log | 500 `ProblemDetails` (not swallowed) |
| Response already started | returns `false` | framework aborts the connection |
| Background service shutdown | handled inside the hosted service via `stoppingToken` | never enters the HTTP exception handler |

**Middleware ordering verified:** `UseExceptionHandler` is the outermost handler (`Program.cs`), wrapping `UseStatusCodePages`, routing, authentication, authorization, and controllers. The status-code-pages writer already returns early when `RequestAborted` is cancelled, so a 499 set by the exception handler cannot trigger a second write.

**Unrelated-request isolation:** the handler holds no mutable static state; only per-request `HttpContext` and the injected logger. Background services cancel on `stoppingToken`, not `HttpContext.RequestAborted`.

### 10.2 Resolved Result-failure inventory

Recursive scan of **156** `.cs` files under `Application/` (excluding `obj/` and `bin/`):

| Measure | Count |
|---|---:|
| Direct `Result.Failure(` | **26** |
| `.FromFailure(` total | **42** |
| — typed `Result<T>.FromFailure(` | **36** (33 single-line + 3 nested/split constructors: `PersonService.cs:103`, `PersonService.cs:292`, `UserService.cs:301`) |
| — non-generic `Result.FromFailure(` (propagation only) | **6** (`ApplicationService.cs:166,190`; `DetainedLicenseService.cs:211,238`; `LocalDrivingLicenseApplicationService.cs:341`; `TestWorkflowService.cs:217`) |
| `Result<T>.FromResult(` propagation | **10** |
| `PropagateFailure<T>` helper definitions | **4** (`LicenseIssuanceService.cs:288`, `LicenseRenewalService.cs:237`, `LicenseReplacementService.cs:210`, `InternationalService.cs:287`) |
| `PropagateFailure` references (definitions + call sites) | **24** |
| Test-only `Result.Failure(` | **13** |
| Test-only `.FromFailure(` | **32** |

**Reconciliation of the “43” figure:** at baseline `5ce6621` the repository had **43** `.FromFailure(` call sites. The reviewed commit changed `AuthService`’s invalid-credentials result from `FromFailure` to `FromUnauthorized`, so current `HEAD` has **42**. The earlier “143” was a combined regular expression over several different failure factories and was never a count of a single factory.

**Direct `Result.Failure` sites (26):** `ApplicationService.cs:221`; `ApplicationTypeService.cs:108,117`; `DetainedLicenseService.cs:245,272,278`; `DriverService.cs:242,291`; `LicenseClassService.cs:115,122`; `LocalDrivingLicenseApplicationService.cs:332,396,402,406,481`; `PersonService.cs:375,418,428,583`; `TestAppointmentService.cs:268,521,572`; `TestTypeService.cs:91`; `UserService.cs:481,554,617`.

**Classification outcome:** every one of the 26 direct sites is a persistence/consistency/configuration failure (zero rows affected after a genuine change, repository update returned false, missing parent aggregate, missing unit-of-work registration). All correctly map to **500** with a generic client detail; the internal reason is logged. No genuine client-correctable business error was found among them, so **no mapping was changed** in this review.

The only business-condition misclassification found in the whole review was the login contract, and it was already corrected in the reviewed commit (`FromFailure` → `FromUnauthorized` → 401). No `Result.Failure` site was reclassified from 500 to 400/409.

**Known residual (unchanged):** `ApplicationService.UpdateApplicationAsync` has no no-op guard, so an update that changes nothing can produce `SaveChanges == 0` and therefore a 500 from the shared `SaveAsync` helper. This is a pre-existing idempotency/semantics issue recorded as `CAND-04`, not an error-classification change; correcting it would change business behavior and was intentionally left out of this focused review.

### 10.3 Tests executed

| Command scope | Result |
|---|---|
| `API.IntegrationTests` filter: `GlobalExceptionHandlerTests`, `ResultHttpMapperTests`, `ProblemDetailsContractTests` | **26 passed, 0 failed, 0 skipped** |
| `Application.UnitTests` filter: all service classes asserting `ErrorType.Failure` + `AuthServiceTests` + cancellation tests | **676 passed, 0 failed, 0 skipped** |

New regression tests added with the cancellation correction:

- `GlobalExceptionHandlerTests.ClientAborted_NonCancellationException_DoesNotWriteResponse`
- `GlobalExceptionHandlerTests.ApplicationCancellation_WithLiveClient_IsNotSwallowed_AndReturnsSafe500`

No SQL-backed test was executed.

### 10.4 Remaining limitations

- SQL integration suites remain unexecuted in this environment (`DVLD_TEST_CONNECTION`/`DVLD_TEST_EXPECTED_INSTANCE` unset); the fixtures fail fast.
- The `ApplicationService.UpdateApplicationAsync` no-op case is documented, not corrected.
- 499 observability depends on the hosting/APM layer recording the status; no application-level metric was added.
