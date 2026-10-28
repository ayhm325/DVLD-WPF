# DVLD-WPF — Documentation Audit (`DOCUMENTATION-AUDIT.md`)

**Origin:** Phase 2 deep source-based verification, with Phase 3 status notes and post-review corrections added
**Phase 2 Audit Timestamp:** 2026-10-28
**Reference Repository:** `https://github.com/ayhm325/DVLD-WPF`
**Local Repository Root:** `D:\DVLD-WPF`

---

## 0. How to Read This Document (Historical vs. Current State)

- **Historical (Phase 2):** Sections 1, 4, 5 and 6 record what was observed at HEAD `4f75658` **before** any documentation edits. Statements such as "undocumented", "omits", "clean working tree", or "planned changes" describe that Phase 2 snapshot and are kept as an audit trail.
- **Current (after Phase 3):** The documentation files were updated in the working tree (uncommitted) to address those findings. Section 0.1 gives the current status of each finding. Where a later review found an audit claim unsupported by source, the claim was corrected in place and marked.
- **Source of truth:** Application source code at HEAD, not earlier documentation. Source-code defects reported in §4 (for example the React items in P1-1, the `ClientApp` CI gap, and the unused `BCrypt.Net-Next` reference) are **not** changed by documentation edits and remain present in source.

### 0.1 Current Status of Phase 2 Findings (after Phase 3 and post-review corrections)

| Finding | Current Status | Where It Is Now Documented / Notes |
|---|---|---|
| P0-1 React `ClientApp` and `"ReactApp"` CORS undocumented | Resolved in docs | `README.md`, `ClientApp/README.md`, `ClientApp/docs/*.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/SECURITY.md`, `docs/SYSTEM-DOCUMENTATION.md` |
| P0-2 `UnitOfWork` transaction behavior undocumented | Resolved in docs | `docs/ARCHITECTURE.md`, `docs/SYSTEM-DOCUMENTATION.md`, `docs/DATABASE.md` §9, `docs/BUSINESS-WORKFLOWS.md` §12 |
| P0-3 HTTP `499` undocumented | Resolved in docs | `docs/API.md`, `docs/SECURITY.md`, `docs/TESTING.md` |
| P0-4 `StaffOnly` excludes `Admin` | Documented; source behavior unchanged | `docs/API.md`, `docs/SECURITY.md` |
| P0-5 Wrong solution path / root CLI commands | Resolved in docs | `README.md`, `docs/TESTING.md`, `docs/SYSTEM-DOCUMENTATION.md` |
| P1-1 React `ClientApp` gaps and lint error | Source defects remain; documented | `ClientApp/docs/FEATURE-STATUS.md`, `ClientApp/docs/DEVELOPMENT.md` |
| P1-2 SQL views / `EnableRetryOnFailure()` | `EnableRetryOnFailure()` documented. **The SQL-views claim was withdrawn**: no views exist in source | `docs/DATABASE.md` §2 and §9 |
| P1-3 Ambiguous-commit verification undocumented | Resolved in docs | `docs/BUSINESS-WORKFLOWS.md` §12 |
| P2-1 JwtBearer integration tests omitted | Resolved in docs | `docs/TESTING.md`, `docs/SECURITY.md` |
| P2-2 Outdated test counts | Resolved in docs (source counts are not executed results) | `README.md`, `docs/TESTING.md`, `docs/SYSTEM-DOCUMENTATION.md` |
| P2-3 CI does not build `ClientApp`; unused `BCrypt.Net-Next` reference | CI gap documented; both source facts unchanged | `README.md`, `docs/TESTING.md`, `docs/SYSTEM-DOCUMENTATION.md` |

---

## 1. Commit and Working-Tree Baseline

| Baseline Item | Verified Value | Evidence / Command |
|---|---|---|
| **Local Repository Path** | `D:\DVLD-WPF` | `Test-Path "D:\DVLD-WPF\.git"` → `True` |
| **Current Branch** | `main` | `git branch --show-current` → `main` |
| **HEAD Commit** | `4f75658f31d634871d9a481a5623519e9dea83e0` | `git rev-parse HEAD` (matches reference commit) |
| **HEAD Commit Subject** | `Add React frontend and configure API CORS` | `git log -1 --oneline` |
| **Recent Commit History** | `4f75658` → `61cbdcc` → `3056f88` → `b5dc945` → `400a8dd` | `git log -5 --oneline` |
| **Pre-Phase-2 Working Tree** | Historical: clean at the start of Phase 2 (no modified, staged, or untracked files) | `git status -s` → empty (Phase 2 snapshot only) |
| **Phase 2 File Modifications** | Historical: only `docs/DOCUMENTATION-AUDIT.md` was created during Phase 2 | Verified via `git status -s` at the end of Phase 2 |
| **Current Working Tree (post-Phase 3)** | Uncommitted documentation changes: 9 modified tracked Markdown files plus 5 untracked Markdown files (`docs/DOCUMENTATION-AUDIT.md` and four `ClientApp/docs/*.md` files); no source, test, project, configuration, or pipeline file changed | `git status --short --untracked-files=all` |

---

## 2. Coverage Inventory and Reviewed Areas

### 2.1 Solution & Project Inventory
The repository contains **9 .NET 8 projects** registered in `Presentation (WPF)/DVLD_WPF.sln`, **1 React/TypeScript/Vite frontend** in `ClientApp/`, **1 CI pipeline** (`azure-pipelines.yml`), and **9 existing Markdown documentation files**.

| Layer / Project | Project File / Entry | Target / Stack | Reviewed Scope |
|---|---|---|---|
| **Domain** | `Domain/Domain.csproj` | `net8.0` | All 14 entities (`Domain/Entities/*.cs`) and 6 enums (`Domain/Enums/*.cs`) |
| **Application** | `Application/Application.csproj` | `net8.0` | All 21 services (`Application/Services/*.cs`), 13 validators (`Application/Validators/*.cs`), 11 mappers (`Application/Mappings/*.cs`), 36 interfaces (`Application/Interfaces/*.cs`), DTOs, `Result`/`Result<T>`, `JwtOptions` |
| **Infrastructure** | `Infrastructure/Infrastructure.csproj` | `net8.0` | `DVLDDbContext.cs`, `DVLDDbContextFactory.cs`, `UnitOfWork.cs`, all 14 entity configurations (`Infrastructure/Configurations/*.cs`), all 15 repositories (`Infrastructure/Repositories/*.cs`), and 4 migrations (`Infrastructure/Migrations/*.cs`) |
| **Contracts** | `DVLD.Contracts/DVLD.Contracts.csproj` | `net8.0` | Transport request/response models across 16 feature folders |
| **Web API** | `API/DVLD.Api/DVLD.Api.csproj` | `net8.0` | `Program.cs`, `appsettings.json`, `Properties/launchSettings.json`, all 20 controllers (`API/DVLD.Api/Controllers/*.cs`), `Results/ResultHttpMapper.cs`, `Security/GlobalExceptionHandler.cs`, `Security/JwtTokenService.cs`, `Security/ApiCurrentUserService.cs` |
| **WPF Client** | `Presentation (WPF)/Presentation.csproj` | `net8.0-windows` | `App.xaml.cs`, `MainWindow.xaml.cs`, `LoginWindow.xaml.cs`, `Services/ApiHostService.cs`, `Services/CurrentUserService.cs`, `Services/Api/ApiClient.cs`, 20 feature API clients, 26 ViewModels registered in `App.xaml.cs` (28 `*ViewModel.cs` files on disk, including `IssueDrivingLicenseForTheFirstTimeViewModel.cs` (in `Presentation (WPF)/ViewModels/`) and `ApplicationInfoViewModel.cs` (in `Presentation (WPF)/ControlViewModels/`, outside `ViewModels/`), which are not registered there), 18 Windows, 8 Pages, 12 UserControls |
| **React Frontend** | `ClientApp/package.json` | React `19.3.1`, TS `~6.0.3`, Vite `8.3.4` | All source/config files: `vite.config.ts`, `eslint.config.js`, `tsconfig*.json`, `src/main.tsx`, `src/App.tsx`, `src/types/Person.ts`, all 5 components (`src/components/*.tsx`), and all 5 services (`src/services/*.ts`) |
| **Unit Tests** | `Test/Application.UnitTests/Application.UnitTests.csproj` | `net8.0` (xUnit + Moq) | Project config + all 21 service test classes (`Test/Application.UnitTests/Services/*.cs`) |
| **API Integration Tests** | `Test/API.IntegrationTests/API.IntegrationTests.csproj` | `net8.0` (xUnit + WebApplicationFactory) | `ApiWebApplicationFactory.cs`, `SqlServerApiWebApplicationFactory.cs`, `TestAuthenticationHandler.cs`, `AuthControllerTests.cs`, `GlobalExceptionHandlerTests.cs`, 20 controller suites, 9 workflow suites |
| **Infra Integration Tests** | `Test/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj` | `net8.0` (xUnit + SQL Server) | `Fixtures/SqlServerTestDatabase.cs`, `UnitOfWorkTests.cs` (full 819 lines), `SqlServerTestDatabaseTests.cs`, 15 repository test classes |
| **CI & Docs** | `azure-pipelines.yml`, `README.md`, `ClientApp/README.md`, `docs/*.md` | YAML / Markdown | Read all 9 `.md` files and `azure-pipelines.yml` line-by-line |

---

## 3. Verified Architecture and Feature-Status Matrix

### 3.1 Compile-Time Dependency Matrix (Verified from `.csproj` Files)
| Project | Project References | Notable NuGet Packages |
|---|---|---|
| `Domain` | None | None |
| `Application` | `Domain` | `BCrypt.Net-Next` (`4.2.0`), `Microsoft.Extensions.Logging.Abstractions` (`10.0.3`), `Microsoft.Extensions.Options` (`10.0.3`) |
| `Infrastructure` | `Application`, `Domain` | `Microsoft.EntityFrameworkCore` (`8.0.18`), `Microsoft.EntityFrameworkCore.SqlServer` (`8.0.0`), `Microsoft.EntityFrameworkCore.Design` (`8.0.0`), `Microsoft.EntityFrameworkCore.Tools` (`8.0.0`) |
| `DVLD.Contracts` | None | None |
| `DVLD.Api` | `Application`, `DVLD.Contracts`, `Infrastructure` | `Microsoft.AspNetCore.Authentication.JwtBearer` (`8.0.19`), `Swashbuckle.AspNetCore` (`6.6.2`), `Microsoft.EntityFrameworkCore.Design` (`8.0.0`) |
| `Presentation (WPF)` | `DVLD.Contracts` (no direct ref to `DVLD.Api`, `Application`, `Infrastructure`, or `Domain`) | `CommunityToolkit.Mvvm` (`8.4.2`), `MaterialDesignThemes` (`5.3.2`), `Microsoft.Extensions.Http` (`10.0.11`), `BCrypt.Net-Next` (`4.2.0` — unused in Presentation code) |
| `ClientApp` | Standalone npm package (`ClientApp/package.json`) | `react` (`^19.3.1`), `react-dom` (`^19.3.1`), `vite` (`^8.3.4`), `typescript` (`~6.0.3`), `eslint` (`^9.39.4`) |

### 3.2 System & Client Feature-Status Matrix

| Area / Feature | Status | Concrete Code Evidence |
|---|---|---|
| **Web API (20 Controllers, 101 HTTP Actions)** | `Implemented` | `API/DVLD.Api/Controllers/*.cs` (exact counts match `docs/API.md` §6–7) |
| **JWT Authentication & Active-User Check** | `Implemented` | `API/DVLD.Api/Program.cs:26-119`, `API/DVLD.Api/Security/JwtTokenService.cs:18-52`, `Application/Services/AuthService.cs:24-73` |
| **Policy Authorization (`AdminOnly`, `StaffOnly`, `StaffOrAdmin`)** | `Implemented` | `API/DVLD.Api/Program.cs:125-138` (`StaffOnly` requires role `Staff`; `AdminOnly` requires `Admin`; `StaffOrAdmin` requires `Staff` or `Admin`) |
| **Login Rate Limiting (`LoginRateLimit`)** | `Implemented` | `API/DVLD.Api/Program.cs:144-167, 376` (5 requests / 1 min / IP, `QueueLimit = 0`, HTTP `429`) + `AuthController.cs:26` |
| **CORS Policy (`"ReactApp"`)** | `Implemented` | `API/DVLD.Api/Program.cs:283-292, 374` (`WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()`) |
| **Global Exception Handling & HTTP `499`** | `Implemented` | `API/DVLD.Api/Security/GlobalExceptionHandler.cs:12-63` (`499 Client Closed Request` on client-aborted `OperationCanceledException`; `500 ProblemDetails` otherwise) |
| **Deferred-Commit `UnitOfWork` & Ambiguous-Commit Verification** | `Implemented` | `Infrastructure/UnitOfWork.cs:12-630` (`ExecuteInTransactionCoreAsync` L82-192, `VerifyCommitOutcomeAsync` L194-230, `EnsureNoActiveTransaction` L232-239, `UnitOfWorkTransaction` L241-352, `TransactionVerificationTracker` L354-629), `Application/Interfaces/IUnitOfWork.cs:5-27` |
| **Database Schema (14 Tables, 4 Migrations; no SQL views)** | `Implemented` | `Infrastructure/DVLDDbContext.cs` (14 `DbSet<T>` properties; `ApplyConfigurationsFromAssembly` at L138), `Infrastructure/Configurations/*.cs` (14 files), `Infrastructure/Migrations/20260901000000_InitialCreate.cs` through `20260907233340_AddUserRole.cs` (none contains `migrationBuilder.Sql`) |
| **WPF Desktop Client (`Presentation (WPF)`)** | `Implemented` | `Presentation (WPF)/App.xaml.cs:80-202`, `MainWindow.xaml.cs:42-375`, `Services/ApiHostService.cs:12-101`, `Services/Api/ApiClient.cs:10-140` |
| **React `ClientApp` — Login (`POST /api/Auth/login`)** | `Implemented` | `ClientApp/src/components/Login.tsx:9-72`, `ClientApp/src/services/authService.ts:22-38`, token stored in React state (`ClientApp/src/App.tsx:19`) |
| **React `ClientApp` — People Directory & Search** | `Implemented` | `ClientApp/src/components/PeopleTable.tsx:11-85`, `ClientApp/src/services/peopleService.ts:3-16`, client-side filter in `ClientApp/src/App.tsx:101-114` |
| **React `ClientApp` — Add Person Modal** | `Partially Implemented` | `ClientApp/src/components/AddPersonModal.tsx:16-429` + `personService.ts:23-71` + `countryService.ts:9-25`: creates person via `POST /api/People`, **but** photo selection (`AddPersonModal.tsx:64-99`) is preview-only and hardcodes `imagePath: null` (`AddPersonModal.tsx:181`) |
| **React `ClientApp` — Dashboard View** | `Partially Implemented` | `ClientApp/src/App.tsx:181-246`: displays live `people.length` and `drivers.length`, **but** hardcodes `0` with `"Connect API to load data"` for `Licenses` (`lines 207-215`) and `Applications` (`lines 218-226`) instead of calling `GET /api/Dashboard/statistics` |
| **React `ClientApp` — Drivers View** | `Partially Implemented` | `ClientApp/src/App.tsx:272-290`, `ClientApp/src/services/driverService.ts:14-30`: fetches `GET /api/Drivers`, **but** renders a `.placeholder-panel` showing only `"Total drivers: {drivers.length}"` without a data table |
| **React `ClientApp` — Settings Pages (`Application Types`, `License Classes`, `Test Types`, `Countries`)** | `Placeholder` | `ClientApp/src/App.tsx:293-333`: renders static descriptions inside `.placeholder-panel` with a `"Back to Dashboard"` button |
| **React `ClientApp` — `Licenses` & `Applications` Pages** | `Placeholder` (Broken View) | `ClientApp/src/components/Sidebar.tsx:62-63` exposes `Licenses` and `Applications` nav buttons, but `ClientApp/src/App.tsx:336-343` excludes both from the fallback placeholder condition without rendering any component for them, resulting in a blank main content area |
| **React `ClientApp` — User Identity Display in Chrome** | `Placeholder` | `ClientApp/src/components/Sidebar.tsx:95-102` hardcodes `"Administrator"` / `"System Admin"` and `Topbar.tsx:20` hardcodes avatar `"A"`; `Login.tsx:21-22` discards `fullName`, `userName`, and `role` from `LoginResponse` |

---

## 4. P0 / P1 / P2 Findings with Evidence and Confidence

### P0 Findings (Architecture, Security, API, EF Core, Transactions)

#### P0-1 (Historical Phase 2 finding; resolved in Phase 3): `ClientApp` (React Frontend) and `"ReactApp"` CORS Policy Were Undocumented Across System Docs at HEAD `4f75658`
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - Commit `4f75658` added `ClientApp/` (`ClientApp/package.json`, `ClientApp/src/App.tsx`) and registered CORS policy `"ReactApp"` allowing `http://localhost:5173` in `API/DVLD.Api/Program.cs:283-292` and `app.UseCors("ReactApp")` at `API/DVLD.Api/Program.cs:374`.
  - At the Phase 2 snapshot, none of `README.md`, `docs/SYSTEM-DOCUMENTATION.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/SECURITY.md`, or `docs/TESTING.md` mentioned `ClientApp` or CORS (Phase 3 added them).
  - At the Phase 2 snapshot, `ClientApp/README.md` (`lines 1-75`) was the unmodified Vite template (`# React + TypeScript + Vite`); Phase 3 replaced it.

#### P0-2 (Historical Phase 2 finding; resolved in Phase 3): `UnitOfWork` Transaction Documentation Omitted Deferred Physical Commit, Rollback on Failure `Result`, Nested-Transaction Guard, and Ambiguous-Commit Verification
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - At the Phase 2 snapshot, `docs/ARCHITECTURE.md:135-158` and `docs/SYSTEM-DOCUMENTATION.md:236-249` stated only that `ExecuteInTransactionAsync` wraps the EF Core execution strategy, rolls back on exception, and leaves `SaveChanges` and `Commit` to the callback (line numbers refer to the pre-Phase 3 files).
  - In current source (`Infrastructure/UnitOfWork.cs`, 630 lines, and `Application/Interfaces/IUnitOfWork.cs:5-27`; line numbers re-verified after Phase 3):
    1. **Nested-transaction guard:** `EnsureNoActiveTransaction()` (`Infrastructure/UnitOfWork.cs:232-239`, called at L29, L44, L89, and L101) throws `InvalidOperationException("A transaction is already active on this DbContext instance. Nested transactions are not supported.")` when a transaction is already active on the `DbContext`.
    2. **Deferred physical commit inside `ExecuteInTransactionCoreAsync` (L82-192):** The `UnitOfWorkTransaction` is created with `deferPhysicalCommit: true` (L110-114). Calling `transaction.CommitAsync()` (L272-293) flushes pending tracked entities via `_context.SaveChangesAsync` (L284), skips `IDbContextTransaction.CommitAsync` while `_deferPhysicalCommit` is set, and sets `_isCompleted = true` (L292). The physical SQL commit is issued afterwards by `CommitPhysicalAsync` (L295-304, called at L149) once the operation delegate has returned successfully.
    3. **Rollback + `ChangeTracker.Clear()` on `Result.IsFailure`:** If `operation` returns `Result { IsFailure: true }` (or the transaction was already rolled back), `ExecuteInTransactionCoreAsync` rolls back (L127) and calls `_context.ChangeTracker.Clear()` (L130) before returning the result (L122-133). Exceptions thrown before the commit stage roll back and clear the tracker in the `catch when (!commitStageReached)` block (L171-190).
    4. **Ambiguous-commit recovery:** If `transaction.CommitPhysicalAsync` throws (L149), the `catch` block (L152-169) disposes the transaction, clears the change tracker, and calls `VerifyCommitOutcomeAsync` (L157; defined L194-230). `VerifyCommitOutcomeAsync` first throws an `InvalidOperationException` ("...no verification probe is available to safely retry.") when no `verifySucceeded` callback was supplied **and** `tracker.CanVerify` is false (L199-204). Otherwise it runs inside a new execution strategy, clears the change tracker (L212), and uses the `verifySucceeded` callback (L214-216; `IUnitOfWork.cs:22-26`) or `TransactionVerificationTracker.VerifyCommittedAsync` (L219; defined L392). `TransactionVerificationTracker` (class at L354) subscribes to `SavingChanges`/`SavedChanges` (L370-371; handlers L456 and L506) and records added, modified, and deleted entity probes; `CanVerify` (L374-377) is true only if at least one such probe was recorded. If verification shows the commit succeeded, the original result is returned without re-running the delegate (L162-165); otherwise the original commit exception is rethrown (L167) so the execution strategy can retry.
    5. **Callers of `verifySucceeded` (corrected after independent audit):** No production service in `Application/Services/*.cs` passes an explicit `verifySucceeded` callback. Production workflows call the three-parameter `ExecuteInTransactionAsync(operation, IsolationLevel.Serializable, ct)` overload (`IUnitOfWork.cs:17-20`), which forwards `verifySucceeded: null` (`UnitOfWork.cs:57-65`) and relies on `TransactionVerificationTracker`; that tracker can verify a commit only when `tracker.CanVerify` is true (L199, L374-377). The four-parameter overload with `verifySucceeded` exists on `IUnitOfWork` (L22-26) and `UnitOfWork` (L67-80) and is exercised separately in `Test/Infrastructure.IntegrationTests/UnitOfWorkTests.cs`.

#### P0-3: `GlobalExceptionHandler` Returns HTTP `499 Client Closed Request` for Client-Canceled Requests
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - `API/DVLD.Api/Security/GlobalExceptionHandler.cs:17-32` checks `if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)`, logs at `Information`, and sets `context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest` (`499`) without writing a `500 ProblemDetails` payload.
  - Tested in `Test/API.IntegrationTests/Middleware/GlobalExceptionHandlerTests.cs`.
  - At the Phase 2 snapshot this was undocumented in `docs/API.md:73, 307-311` and `docs/SECURITY.md:93-105` (pre-Phase 3 line numbers); Phase 3 documented it.

#### P0-4: Strict `StaffOnly` vs `AdminOnly` Policy Separation Causes `403 Forbidden` for `Admin` Users in `ClientApp`
- **Type:** Verified Fact (Code Behavior) + Inference (UX Impact) | **Confidence:** `High`
- **Evidence:**
  - `API/DVLD.Api/Program.cs:125-138` defines:
    - `"AdminOnly"` → `policy.RequireRole("Admin")`
    - `"StaffOnly"` → `policy.RequireRole("Staff")` (does **not** include `"Admin"`)
    - `"StaffOrAdmin"` → `policy.RequireRole("Staff", "Admin")`
  - In `Presentation (WPF)/MainWindow.xaml.cs:84-115` (`ApplyRoleVisibility`), the WPF UI hides operational navigation items (including `People` and `Drivers`) when logged in as `Admin`, and hides `Users`, `Application Types`, and `Test Types` when logged in as `Staff`.
  - However, in `ClientApp/src/App.tsx:67-98`, logging in or viewing the Dashboard/Drivers pages unconditionally calls both `getPeople(accessToken)` (`GET /api/People`, `StaffOrAdmin`) and `getDrivers(accessToken)` (`GET /api/Drivers`, `DriversController.cs:16` `StaffOnly`). Consequently, an `Admin` user logging into `ClientApp` receives `HTTP 403 Forbidden` from `GET /api/Drivers`, triggering `driversError` (`Failed to load drivers (403)`).
  - `docs/SECURITY.md:54-66` and `docs/API.md:47-53` list the policy table but do not explain that `StaffOnly` strictly excludes `Admin` (unlike a hierarchical role model).

#### P0-5 (Historical Phase 2 finding; resolved in Phase 3): Incorrect Solution Directory Name and Broken Root CLI Commands in `README.md` and `docs/`
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - On disk, the WPF directory is `Presentation (WPF)/` and the solution file is `Presentation (WPF)/DVLD_WPF.sln` (there is no `.sln` file at the repository root `D:\DVLD-WPF`).
  - `README.md:51` and `docs/SYSTEM-DOCUMENTATION.md:27` list the folder as `Presentation/`.
  - `README.md:90-91, 100` and `docs/TESTING.md:205` instruct users to run `dotnet restore`, `dotnet build --configuration Release`, and `dotnet test --configuration Release` without a project/solution argument, which fails when executed from the repository root (`MSHB1003: Specify a project or solution file`). Compare with `azure-pipelines.yml:34-46`, which correctly passes `"Presentation (WPF)/DVLD_WPF.sln"`.

---

### P1 Findings (Business Workflows, WPF/MVVM, React `ClientApp`, Data Access)

#### P1-1: React `ClientApp` Contains Partially Implemented Views, Placeholder Pages, a Blank Navigation Bug, and a Failing ESLint Rule
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - **Broken `Licenses` and `Applications` navigation (`ClientApp/src/App.tsx:336-343`):** `Sidebar.tsx:62-63` allows selecting `'Licenses'` and `'Applications'`, but `App.tsx:336-343` checks `!['Dashboard', 'People', 'Drivers', 'Licenses', 'Applications', ...settingsPages].includes(activePage)` and provides no render block for `'Licenses'` or `'Applications'`, leaving the main content area completely blank.
  - **Dashboard hardcoded zeros (`ClientApp/src/App.tsx:207-226`):** `Licenses` and `Applications` stat cards hardcode `0` and `"Connect API to load data"` instead of calling `GET /api/Dashboard/statistics` (`DashboardController.cs:13`).
  - **Add Person photo not persisted (`ClientApp/src/components/AddPersonModal.tsx:64-99, 181`):** Selecting a photo only creates a browser `URL.createObjectURL` preview; `handleSubmit` sends `imagePath: null` at line 181.
  - **Client-side validation mismatch (`ClientApp/src/components/AddPersonModal.tsx:142-152`):** Enforces a 10-digit numeric `nationalNo` (`/^\d{10}$/`) and Jordanian mobile `phone` (`/^(077|078|079)\d{7}$/`), whereas backend `Application/Validators/PersonValidator.cs` accepts any non-empty `NationalNo` (`<= 20` chars) and `Phone` (`<= 20` chars).
  - **Split person services:** `ClientApp/src/services/peopleService.ts` (`getPeople`) and `ClientApp/src/services/personService.ts` (`createPerson`) split `/api/People` calls across two files.
  - **ESLint error on `npm run lint`:** `ClientApp/src/components/AddPersonModal.tsx:67:7` fails `react-hooks/set-state-in-effect` (`setImagePreview('')` called synchronously inside `useEffect`).

#### P1-2 (Corrected after review): `EnableRetryOnFailure()` Runtime Details Were Not Documented; the Earlier "Two SQL Views" Claim Was Unsupported and Has Been Withdrawn
- **Type:** Verified Fact (retry configuration) / Retracted Claim (SQL views) | **Confidence:** `High`
- **Evidence:**
  - `API/DVLD.Api/Program.cs` configures `options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())`. At the Phase 2 snapshot this was not documented in `docs/DATABASE.md`; Phase 3 documented it in §9.
  - **Retracted:** The Phase 2 text claimed that `20260901000000_InitialCreate.cs` creates `LocalDrivingLicenseApplications_View` and `DetainedLicenses_View` and that two repositories query them. Re-verification against source found no support: `DVLDDbContext` has no keyless/`ToView` mappings, none of the 14 entity configurations or 4 migrations contains view creation or `migrationBuilder.Sql`, and no `_View` identifier exists in `Infrastructure/`, `Application/`, or `Domain/`.
  - **Actual data access:** 14 `DbSet<T>` tables; repositories use EF Core LINQ; raw SQL appears only as `FromSqlInterpolated` `UPDLOCK` row-lock reads on base tables (`ApplicationRepository.cs`, `DetainedLicenseRepository.cs`, `LicenseRepository.cs`, `TestAppointmentRepository.cs`) and one `SqlQueryRaw` aggregate query in `DashboardRepository.cs`. See `docs/DATABASE.md` §2.

#### P1-3 (Historical Phase 2 finding; resolved in Phase 3): `docs/BUSINESS-WORKFLOWS.md` Omitted Ambiguous-Commit Verification and `ChangeTracker.Clear()` Rollback Guarantees
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - At the Phase 2 snapshot, `docs/BUSINESS-WORKFLOWS.md` (§3–9, §12) described workflow steps (`SaveChanges → Commit`) without noting that `IUnitOfWork.ExecuteInTransactionAsync` defers physical commit until the workflow returns `Result.Success`, automatically rolls back and clears `DVLDDbContext.ChangeTracker` on `Result.IsFailure` (`Infrastructure/UnitOfWork.cs:122-133`), and uses `TransactionVerificationTracker` (automatically, via the three-parameter `ExecuteInTransactionAsync` overload used by production services, when `tracker.CanVerify` allows verification; the explicit `verifySucceeded` overload is tested separately in `UnitOfWorkTests.cs`) to prevent duplicate execution if a connection drops during commit. Phase 3 documented this in §12.

---

### P2 Findings (Tests, Test Auth, CI/CD, Package Hygiene)

#### P2-1 (Historical Phase 2 finding; resolved in Phase 3): `docs/TESTING.md` and `docs/SECURITY.md` Inaccurately Claimed Integration Tests Never Exercise Real `JwtBearer` Validation
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - `docs/TESTING.md:187-201` and `docs/SECURITY.md:169` state that integration tests use `TestAuthenticationHandler` and do not test real JWT cryptographic/token validation.
  - In reality, `Test/API.IntegrationTests/Controllers/AuthControllerTests.cs:406-503` contains two integration tests (`JwtBearer_WhenTokenIsValidAndUserIsActive_AllowsAccessViaUserService` and `JwtBearer_WhenUserIsInactiveOrMissing_ReturnsUnauthorizedViaUserService`) that post-configure `DefaultAuthenticateScheme` and `DefaultChallengeScheme` to `JwtBearerDefaults.AuthenticationScheme`, mint a real signed JWT with `IJwtTokenService`, and verify both cryptographic validation and the `OnTokenValidated` active-user check via `IUserService.IsUserActiveAsync`.

#### P2-2 (Historical Phase 2 finding; resolved in Phase 3): Outdated Historical Test Counts (`1,487`) Across `README.md`, `docs/SYSTEM-DOCUMENTATION.md`, and `docs/TESTING.md`
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - `README.md:125-130`, `docs/SYSTEM-DOCUMENTATION.md:344-350`, and `docs/TESTING.md:222-227` list historical counts of `631` / `364` / `492` (`1,487` total).
  - Current verified source test case counts (`[Fact]` + `[InlineData]`) and VSTest discovery/execution counts:

| Test Project | Test Files | `[Fact]` | `[Theory]` | `[InlineData]` | Source Test Cases (`[Fact]+[InlineData]`) | VSTest Discovered / Executed (`--no-build`) |
|---|---:|---:|---:|---:|---:|---:|
| `Application.UnitTests` | 21 | 569 | 34 | 73 | **642** | **633 Passed** (0 Failed, 5s) |
| `API.IntegrationTests` | 29 suites (+4 infra) | 314 | 48 | 128 | **442** | **435 Discovered** (`--list-tests`) |
| `Infrastructure.IntegrationTests` | 17 suites (+1 fixture) | 439 | 30 | 68 | **507** | **494 Discovered** (`--list-tests`) |
| **Total** | **67 suites** | **1,322** | **112** | **269** | **1,591** | **1,562** |

  - **Why the counts differ (corrected after independent audit):** The VSTest numbers (`633` / `435` / `494` = `1,562`) came from `--no-build` runs against the existing `bin/Debug/net8.0` assemblies, built on 2026-10-26 (12:39–15:21), before commits `b5dc945`, `3056f88`, and `61cbdcc` added 29 source test cases. The source counts at `HEAD` (`642` / `442` / `507` = `1,591`) are static attribute counts, not executed test results. Only the `633` `Application.UnitTests` entries were executed; the API and Infrastructure suites were enumerated only.

  - Also, `docs/TESTING.md:135-143` (§7 "Unit of Work tests") lists only 4 basic items, omitting the 11 tests in `Test/Infrastructure.IntegrationTests/UnitOfWorkTests.cs` covering failure-Result rollback, `ChangeTracker.Clear()`, nested-transaction rejection, pre-commit retry, and post-commit ambiguous failure recovery via `FaultInjectingTransactionInterceptor`.

#### P2-3: `azure-pipelines.yml` Does Not Build or Lint `ClientApp/`, and `Presentation.csproj` Has an Unused `BCrypt.Net-Next` Reference
- **Type:** Verified Fact | **Confidence:** `High`
- **Evidence:**
  - `azure-pipelines.yml:1-66` restores, builds, and tests `"Presentation (WPF)/DVLD_WPF.sln"` on `windows-latest` using `.NET 8.0.x`, with no Node.js / `ClientApp` `npm ci`, `npm run lint`, or `npm run build` step.
  - `Presentation (WPF)/Presentation.csproj:24` includes `<PackageReference Include="BCrypt.Net-Next" Version="4.2.0" />`, although password hashing is performed exclusively in `Application/Services/AuthService.cs:46` and `Application/Services/UserService.cs:244, 420, 432`.

---

## 5. Documentation Gaps and Recommended Changes (Prioritized Update Checklist)

### 5.1 Architectural Documentation Decision — Separating React Documentation from Core System Documentation

**Decision:** Separate detailed React (`ClientApp`) documentation from the main `.NET` / `WPF` system documentation in `docs/`.

**Rationale:**
1. **High Rate of Frontend Change:** React (`ClientApp`) development started recently (commit `4f75658`) and is expected to evolve rapidly across upcoming iterations.
2. **Stability of Core Backend & WPF Docs:** Keeping `docs/` focused on the `.NET 8` backend (`Domain`, `Application`, `Infrastructure`, `DVLD.Contracts`, `DVLD.Api`), database, security, workflows, tests, and `Presentation (WPF)` ensures the core architectural documentation remains stable and focused on its responsibilities.
3. **Independent Frontend Documentation Lifecycle:** Hosting React-specific documentation inside `ClientApp/README.md` and `ClientApp/docs/` allows the frontend documentation to be updated independently with each stage of UI development without churning backend documentation files.

**Documentation Boundary Rules for Phase 3:**
- **Root `README.md`:** Remains the single entry point for the entire repository, introducing the `.NET 8` backend, `Presentation (WPF)` desktop client, and `ClientApp` React frontend, and linking to both `docs/` and `ClientApp/README.md` + `ClientApp/docs/`.
- **Core System Docs (`docs/*.md`):** Remain responsible for overall system documentation, `.NET` layers, HTTP API, database schema/migrations, security policies, business workflows, and automated `.NET` test suites.
- **System-Level Boundary in `docs/ARCHITECTURE.md` (and `docs/SYSTEM-DOCUMENTATION.md`):** Documents the relationship between `ClientApp` (React), `DVLD.Api`, and `Presentation (WPF)` **at the system boundary level only** (`HTTP + JSON + JWT` over `http://localhost:5260/api` and the `"ReactApp"` CORS policy for `http://localhost:5173`), without duplicating internal React component/state/service details, and delegates internal frontend details to `ClientApp/docs/`.
- **React Entry Guide (`ClientApp/README.md`):** Replaces the default Vite boilerplate with a focused guide for running, building, linting, and developing the React app, linking to `ClientApp/docs/`.
- **Detailed React Documentation (`ClientApp/docs/`):** Dedicated frontend documentation under `ClientApp/docs/` covering:
  1. **Frontend Architecture** (`ClientApp/docs/ARCHITECTURE.md`) — component hierarchy, state flow, styling, and folder structure.
  2. **API Integration & Authentication** (`ClientApp/docs/API-INTEGRATION.md`) — service modules (`authService`, `peopleService`, `personService`, `driverService`, `countryService`), base URL (`http://localhost:5260/api`), CORS (`http://localhost:5173`), in-memory JWT `accessToken` handling, and role/policy interaction (`StaffOrAdmin` vs `StaffOnly`).
  3. **Feature Status Matrix** (`ClientApp/docs/FEATURE-STATUS.md`) — explicit, evidence-based breakdown of what is **Completed (`Implemented`)**, **Partial (`Partially Implemented`)**, and **Not Implemented (`Placeholder` / `Not Implemented`)**.
  4. **Development Guide** (`ClientApp/docs/DEVELOPMENT.md`) — prerequisites, scripts (`dev`, `build`, `lint`, `preview`), TypeScript/ESLint configuration, known issues (including `AddPersonModal.tsx:67` ESLint error and `Licenses`/`Applications` routing gap), and conventions for adding new pages/services.

### 5.2 Prioritized Phase 3 Update Checklist

> **Status after Phase 3:** The "Planned Phase 3 Changes" column below is the Phase 2 plan. All ten rows were carried out in the working tree (uncommitted), with these exceptions recorded after review: the "2 SQL views" item in row 4 was withdrawn (no views exist in source), and the explanations of `verifySucceeded` usage and source-vs-VSTest test counts were corrected.

| Order | Target File / Directory | Scope | Priority | Planned Phase 3 Changes |
|---:|---|---|---|---|
| **1** | `docs/ARCHITECTURE.md` | Core System & `.NET` / `WPF` | **P0** | • Document the system-level relationship between `Presentation (WPF)`, `ClientApp` (React), and `DVLD.Api` (shared `HTTP + JSON + JWT` API boundary and `"ReactApp"` CORS policy) without duplicating internal React details; link to `ClientApp/docs/`.<br>• Fix directory reference `Presentation` → `Presentation (WPF)` (`DVLD_WPF.sln`).<br>• Update §8 (`Transaction model`) to document deferred physical commit in `ExecuteInTransactionCoreAsync`, automatic rollback + `ChangeTracker.Clear()` on `Result.IsFailure`, `EnsureNoActiveTransaction()`, and ambiguous-commit verification (`VerifyCommitOutcomeAsync`, `TransactionVerificationTracker`, `verifySucceeded`). |
| **2** | `docs/SECURITY.md` | Core System & API Security | **P0 / P1** | • Add API CORS policy section (`"ReactApp"` allowing `http://localhost:5173` in `Program.cs:283-292, 374`).<br>• Clarify in §5 that `StaffOnly` strictly requires `UserRole.Staff` (excluding `Admin`).<br>• Update §9 to document HTTP `499 Client Closed Request` for client-aborted requests in `GlobalExceptionHandler.cs:17-32`.<br>• Update §15 to document the real `JwtBearer` + `OnTokenValidated` active-user integration tests in `AuthControllerTests.cs:406-503`. |
| **3** | `docs/API.md` | HTTP API Reference | **P1** | • Add CORS policy note (`http://localhost:5173` via `"ReactApp"`) in §2/§3.<br>• Clarify in §4 that `StaffOnly` only permits `UserRole.Staff` (`Admin` is not implicitly included).<br>• Update §5 and §8 to document HTTP `499 Client Closed Request` when `GlobalExceptionHandler` handles client-canceled requests. |
| **4** | `docs/DATABASE.md` | Database & EF Core | **P1** | • Document the actual data access mechanism (14 tables, EF Core LINQ repositories, `UPDLOCK` raw-SQL row-lock reads, and the dashboard `SqlQueryRaw` aggregate); the originally planned "2 SQL views" item was **withdrawn** because no views exist in source.<br>• Add a section on connection resiliency (`EnableRetryOnFailure()` in `Program.cs`) and how `UnitOfWork` coordinates retry execution with ambiguous-commit verification. |
| **5** | `docs/BUSINESS-WORKFLOWS.md` | Business Workflows | **P1 / P2** | • Update §12 (`Transaction and concurrency philosophy`) to explain deferred physical commit, automatic rollback + `ChangeTracker.Clear()` when a workflow step returns a failure `Result`, and ambiguous-commit verification (`verifySucceeded` / `TransactionVerificationTracker`). |
| **6** | `docs/TESTING.md` | `.NET` Test Suites & CI | **P1 / P2** | • Update §3 (`API integration tests`) to list `GlobalExceptionHandlerTests` alongside the 20 controller test suites.<br>• Expand §7 (`Unit of Work tests`) to cover all 11 scenarios in `UnitOfWorkTests.cs` (failure-Result rollback, `ChangeTracker.Clear()`, nested-transaction guard, pre-commit retry, post-commit disconnect verification).<br>• Update §11 to document the real `JwtBearer` integration tests in `AuthControllerTests.cs:406-503`.<br>• Update §12 CLI command (`dotnet test "Presentation (WPF)\DVLD_WPF.sln" --configuration Release`).<br>• Update §13 test table with current verified counts (`642` / `442` / `507` = `1,591` source test cases; `633` / `435` / `494` = `1,562` VSTest entries) and note in §14 that `azure-pipelines.yml` targets the `.NET` solution. |
| **7** | `docs/SYSTEM-DOCUMENTATION.md` | Consolidated System Reference | **P0 / P1** | • Update §2 (`Solution structure`), §3 (`Runtime architecture`), §4 (`Technology stack`), and §6 (`Layer responsibilities`) to reference `Presentation (WPF)/` and note `ClientApp/` at the system boundary level (pointing to `ClientApp/README.md` and `ClientApp/docs/` for React details).<br>• Update §12 (`Unit of Work and transactions`), §15 (`Security`), §17 (`Test counts`), and §22 (`Documentation map`). |
| **8** | `ClientApp/README.md` | React Operational & Dev Entry Guide | **P0** | • Replace stock Vite template with the React startup and development guide: overview, prerequisites (`DVLD.Api` at `http://localhost:5260`), commands (`npm install`, `npm run dev`, `npm run build`, `npm run lint`, `npm run preview`), high-level feature status summary (`Implemented` / `Partially Implemented` / `Not Implemented`), and links to `ClientApp/docs/`. |
| **9** | `ClientApp/docs/` (`ARCHITECTURE.md`, `API-INTEGRATION.md`, `FEATURE-STATUS.md`, `DEVELOPMENT.md`) | Detailed React Documentation | **P0 / P1** | • Create modular React documentation under `ClientApp/docs/` covering:<br>  1. `ARCHITECTURE.md`: Component tree (`App`, `Login`, `Sidebar`, `Topbar`, `PeopleTable`, `AddPersonModal`), state management, routing model, and styling.<br>  2. `API-INTEGRATION.md`: Endpoints consumed, service modules (`authService`, `peopleService`, `personService`, `driverService`, `countryService`), JWT bearer flow (`accessToken` in memory), CORS, and API policy constraints (`StaffOnly` `403` on `/api/Drivers` for `Admin`).<br>  3. `FEATURE-STATUS.md`: Explicit classification of **Completed (`Implemented`)** (Login, People list & search), **Partial (`Partially Implemented`)** (Add Person modal without photo persistence + stricter regexes, Dashboard partial stats, Drivers count-only view), and **Not Implemented (`Placeholder` / `Not Implemented`)** (`Licenses` & `Applications` blank views, Settings placeholder pages, hardcoded sidebar/topbar user identity).<br>  4. `DEVELOPMENT.md`: Local dev workflow, TypeScript/ESLint rules, current ESLint error in `AddPersonModal.tsx:67`, and guide for implementing remaining pages. |
| **10** | `README.md` | Root Project Entry Point | **P0 / P1** | • Keep as the unified entry point for the entire project.<br>• Update architecture diagram and folder structure (`Presentation (WPF)/` and `ClientApp/`).<br>• Fix root `.NET` CLI commands (`"Presentation (WPF)\DVLD_WPF.sln"`), add quick-start pointer to `ClientApp/README.md`, update test counts (`1,591` source test cases / `1,562` VSTest entries), and link to both `docs/` and `ClientApp/docs/`. |

---

## 6. Commands Actually Run and Their Exact Outcomes

Historical record: all commands below were executed during Phase 2, before any Phase 3 edits (for example, row 1 reflects the clean Phase 2 starting state), in `D:\DVLD-WPF` inside the standard read-only/sandboxed workflow:

| # | Command Executed | Exit Code | Exact Outcome |
|---:|---|---:|---|
| 1 | `git branch --show-current; git rev-parse HEAD; git status -s; Test-Path "docs\DOCUMENTATION-AUDIT.md"` | `0` | Branch `main`, HEAD `4f75658f31d634871d9a481a5623519e9dea83e0`, working tree clean, `docs\DOCUMENTATION-AUDIT.md` did not exist (`False`). |
| 2 | `Get-ChildItem -Path "API\DVLD.Api\Controllers\*.cs" \| ... Select-String ...` | `0` | Extracted all `[Authorize]`, `[AllowAnonymous]`, `[EnableRateLimiting]`, `[Route]`, and `[Http*]` attributes across all 20 controllers (verified 20 controllers, 101 HTTP actions). |
| 3 | `dotnet test "Test\Application.UnitTests\Application.UnitTests.csproj" --no-build --verbosity minimal` | `0` | `Passed! - Failed: 0, Passed: 633, Skipped: 0, Total: 633, Duration: 5 s - Application.UnitTests.dll (net8.0)` |
| 4 | `dotnet test "Test\API.IntegrationTests\API.IntegrationTests.csproj" --no-build --list-tests` | `0` | Listed **435** VSTest test entries in `API.IntegrationTests.dll`. |
| 5 | `dotnet test "Test\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj" --no-build --list-tests` | `0` | Listed **494** VSTest test entries in `Infrastructure.IntegrationTests.dll`. |
| 6 | PowerShell source attribute count across `Test\*\*.cs` (excluding `bin\`/`obj\`) | `0` | `Application.UnitTests`: 21 files, 569 `[Fact]`, 34 `[Theory]`, 73 `[InlineData]` (642 total).<br>`API.IntegrationTests`: 33 files, 314 `[Fact]`, 48 `[Theory]`, 128 `[InlineData]` (442 total).<br>`Infrastructure.IntegrationTests`: 18 files, 439 `[Fact]`, 30 `[Theory]`, 68 `[InlineData]` (507 total).<br>Grand total: **1,591** (`[Fact] + [InlineData]`). |
| 7 | `dotnet build "Presentation (WPF)\DVLD_WPF.sln" --verbosity minimal` (and test `.csproj` builds inside sandbox) | `1` | Failed inside the network-isolated sandbox (`NU1900` NuGet vulnerability check / sandbox MSBuild restriction); existing `bin\Debug\net8.0` assemblies were used for `--no-build` execution/discovery. Those prebuilt Debug assemblies (built 2026-10-26, 12:39–15:21) **predate** commits `b5dc945`, `3056f88`, and `61cbdcc`, which added 29 source test cases (`1,562` → `1,591` `[Fact]+[InlineData]` attributes), so the VSTest numbers in rows 3-5 describe the older assemblies. Source attribute counts (row 6) are static counts, **not** executed test results; only the `633` `Application.UnitTests` entries in row 3 were executed. |
| 8 | `npm.cmd --prefix ClientApp run lint` | `0` (wrapper) / `1` (eslint) | Executed `eslint .` in `ClientApp`: reported **1 error, 0 warnings** at `ClientApp/src/components/AddPersonModal.tsx:67:7` (`react-hooks/set-state-in-effect`: `Calling setState synchronously within an effect can trigger cascading renders`). |

---

## 7. Blockers and Unverified Areas

### Blockers
- **Historical (Phase 2):** **None** for completing Phase 2 (`docs/DOCUMENTATION-AUDIT.md`) or proceeding to Phase 3 (documentation updates).

### Explicitly Unverified or Excluded Areas
1. **Live SQL Server Integration Test Execution in Phase 2 (`API.IntegrationTests` & `Infrastructure.IntegrationTests`):**
   - Running these two suites executes `Database.Migrate()` and creates/drops real SQL Server databases using `DVLD_TEST_CONNECTION`. In accordance with Phase 2 safety rules, they were inspected in source and enumerated via `dotnet test --no-build --list-tests` (`435` and `494` discovered tests) rather than executed against a live SQL Server instance during Phase 2.
2. **Generated / Third-Party / IDE Artifacts Excluded from Line-by-Line Review:**
   - `bin/`, `obj/`, `Presentation (WPF)/.vs/`, `ClientApp/node_modules/`, `ClientApp/package-lock.json`, EF Core auto-generated migration `.Designer.cs` files, and `Infrastructure/Migrations/DVLDDbContextModelSnapshot.cs` (verified via entity configurations and migration `Up`/`Down` code).
3. **Interactive GUI & Production Hosting Not Executed:**
   - Did not launch an interactive WPF desktop window (`Presentation.exe`) or Vite browser session (`npm run dev`), and did not inspect external Azure DevOps cloud pipeline run history.
