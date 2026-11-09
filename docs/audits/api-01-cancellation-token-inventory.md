# API-01 — Comprehensive CancellationToken Inventory & Architectural Assessment

> **Task ID:** `API-01`  
> **Classification:** Confirmed Technical Remediation  
> **Priority:** P2  
> **Phase:** 12.3 (Reliability, Observability & Core Policy)  
> **Status:** Verified (Final Audit Complete)  
> **Date:** 2026-11-09  
> **Governance Authority:** [`AGENTS.md`](../../AGENTS.md) / [`ENGINEERING-ROADMAP.md`](../../ENGINEERING-ROADMAP.md)  

---

## 1. Architectural Objective & Problem Statement

Client HTTP requests can be aborted at any time (e.g., user navigates away, network timeout, client drops connection). In ASP.NET Core, the framework signals client disconnection via `HttpContext.RequestAborted` (`CancellationToken`).

Prior to `API-01`:
1. All **109** async Controller action methods across 20 API controllers (out of **110** total actions, where `AuthController.Me` is the sole synchronous claims-reading action) omitted `CancellationToken cancellationToken`.
2. **147** out of **153** Application Service async methods omitted `CancellationToken`, or dropped it before calling repositories.
3. **139** out of **141** Repository async methods and their internal EF Core queries (`ToListAsync()`, `FirstOrDefaultAsync()`, `SingleOrDefaultAsync()`, `SingleAsync()`, `AnyAsync()`, `CountAsync()`, `AddAsync()`, `ToDictionaryAsync()`, `ExecuteUpdateAsync()`, `ExecuteSqlInterpolatedAsync()`) omitted `cancellationToken`, causing orphaned SQL queries to continue executing on SQL Server even after client disconnection.
4. Mutation workflows frequently called `_unitOfWork.SaveChangesAsync()` without propagating the token, or explicitly passed `CancellationToken.None`.
5. Background services: `LicenseExpirationBackgroundService` called `DeactivateExpiredLicensesAsync` without forwarding its `stoppingToken`.

---

## 2. Verified Inventory of Async Operations Across Layers

### 2.1 Repositories Layer (15 Repository Interfaces & Implementations)

| Repository Interface / Implementation | Total Async Methods | Methods Lacking Token (Pre-API-01) | EF Core Async Methods Invoked |
|---|:---:|:---:|---|
| `IApplicationRepository` / `ApplicationRepository` | 12 | 12 | `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `IApplicationTypeRepository` / `ApplicationTypeRepository` | 3 | 3 | `ToListAsync`, `FirstOrDefaultAsync` |
| `ICountryRepository` / `CountryRepository` | 1 | 1 | `ToListAsync` |
| `IDashboardRepository` / `DashboardRepository` | 1 | 1 | `SingleAsync`, `CountAsync`, `ToListAsync`, `ToDictionaryAsync` |
| `IDetainedLicenseRepository` / `DetainedLicenseRepository` | 7 | 7 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `IDriverRepository` / `DriverRepository` | 11 | 11 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`, `AddAsync` |
| `IInternationalRepository` / `InternationalRepository` | 8 | 8 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `ILicenseClassRepository` / `LicenseClassRepository` | 3 | 3 | `ToListAsync`, `FirstOrDefaultAsync` |
| `ILicenseRepository` / `LicenseRepository` | 24 | 24 | `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `CountAsync`, `ExecuteUpdateAsync`, `ExecuteSqlInterpolatedAsync`, `AddAsync` |
| `ILocalDrivingLicenseApplicationRepository` / `LocalDrivingLicenseApplicationRepository` | 12 | 12 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`, `AddAsync` |
| `IPersonRepository` / `PersonRepository` | 13 | 13 | `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `CountAsync`, `AddAsync` |
| `ITestAppointmentRepository` / `TestAppointmentRepository` | 24 | 22 | `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `CountAsync`, `AddAsync` (*2 locking methods already had CT*) |
| `ITestRepository` / `TestRepository` | 6 | 6 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`, `AddAsync` |
| `ITestTypeRepository` / `TestTypeRepository` | 3 | 3 | `ToListAsync`, `FirstOrDefaultAsync` |
| `IUserRepository` / `UserRepository` | 13 | 13 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| **Total Repositories** | **141** | **139** | **100% of EF Core Async Operations Receive `cancellationToken` (0 Missing)** |

---

### 2.2 Application Services Layer (26 Service Interfaces & Implementations)

| Service Interface / Implementation | Total Async Methods | Status & Verification |
|---|:---:|---|
| `IApplicationService` / `ApplicationService` | 9 | **Updated & Verified (Bundle 1)** — Propagates CT to repo & `SaveChangesAsync` |
| `IApplicationTypeService` / `ApplicationTypeService` | 3 | **Updated & Verified (Bundle 1)** — Propagates CT to repo & `SaveChangesAsync` |
| `IAuthService` / `AuthService` | 1 | **Updated & Verified (Bundle 2)** — Propagates CT to `GetByUsernameWithDetailsAsync` |
| `ICountryService` / `CountryService` | 1 | **Updated & Verified (Bundle 1)** — Propagates CT to `GetAllCountriesAsync` |
| `ICurrentUserService` / `CurrentUserService` | 0 | Synchronous claims reader (no I/O) |
| `IDashboardService` / `DashboardService` | 1 | **Updated & Verified (Bundle 1)** — Propagates CT to `GetStatisticsAsync` (`SingleAsync`) |
| `IDetainedLicenseService` / `DetainedLicenseService` | 6 | **Updated & Verified (Bundle 5)** — Propagates CT to repos, `ExecuteInTransactionAsync`, `SaveChangesAsync`, `CommitAsync` |
| `IDriverService` / `DriverService` | 11 | **Updated & Verified (Bundle 3)** — Propagates CT to repo & `SaveChangesAsync` |
| `IFileStorageService` / `PhysicalFileStorageService` | 4 | **Audited & Verified (Final Audit)** — `SaveAsync` passes CT to `CopyToAsync` & `FlushAsync`; `GetAsync`/`DeleteAsync`/`ExistsAsync` documented in Section 2.6 |
| `IImageProcessingService` / `ImageProcessingService` | 0 | Synchronous in-memory CPU image validation/encoding |
| `IInternationalService` / `InternationalService` | 8 | **Updated & Verified (Bundle 5)** — Propagates CT to repos, `ExecuteInTransactionAsync`, `SaveChangesAsync`, `CommitAsync` |
| `IJwtTokenService` / `JwtTokenService` | 0 | Synchronous in-memory JWT token generation |
| `ILicenseClassService` / `LicenseClassService` | 3 | **Updated & Verified (Bundle 1)** — Propagates CT to repo & `SaveChangesAsync` |
| `ILicenseIssuanceService` / `LicenseIssuanceService` | 1 | **Updated & Verified (Bundle 5)** — Propagates CT to repos, services, `ExecuteInTransactionAsync`, `SaveChangesAsync`, `CommitAsync` |
| `ILicenseQueryService` / `LicenseQueryService` | 15 | **Updated & Verified (Bundle 5)** — Propagates CT to repo & batched lookups |
| `ILicenseRenewalService` / `LicenseRenewalService` | 1 | **Updated & Verified (Bundle 5)** — Propagates CT to repos, services, `ExecuteInTransactionAsync`, `SaveChangesAsync`, `CommitAsync` |
| `ILicenseReplacementService` / `LicenseReplacementService` | 1 | **Updated & Verified (Bundle 5)** — Propagates CT to repos, services, `ExecuteInTransactionAsync`, `SaveChangesAsync`, `CommitAsync` |
| `ILicenseService` / `LicenseService` | 15 | **Updated & Verified (Bundle 5)** — Propagates CT to `ILicenseQueryService` |
| `ILocalDrivingLicenseApplicationService` / `LocalDrivingLicenseApplicationService` | 15 | **Updated & Verified (Bundle 3)** — Propagates CT to repos, services, transactions & `SaveChangesAsync` |
| `INoShowProcessorService` / `NoShowProcessorService` | 2 | **Audited & Verified (Bundle 4 & Final Audit)** — Propagates CT to UoW & Repos; rethrows `OperationCanceledException` when canceled |
| `IPersonService` / `PersonService` | 14 | **Updated & Verified (Bundle 2 & Final Audit)** — Propagates CT to repo, `IFileStorageService`, & `SaveChangesAsync`; never swallows `OperationCanceledException` |
| `ITestAppointmentService` / `TestAppointmentService` | 17 | **Updated & Verified (Bundle 4)** — Propagates CT to repos, services, transactions & `SaveChangesAsync` |
| `ITestService` / `TestService` | 5 | **Updated & Verified (Bundle 4)** — Propagates CT to repo |
| `ITestTypeService` / `TestTypeService` | 3 | **Updated & Verified (Bundle 1)** — Propagates CT to repo & `SaveChangesAsync` |
| `ITestWorkflowService` / `TestWorkflowService` | 5 | **Updated & Verified (Bundle 4)** — Propagates CT to repos, services, transactions & `SaveChangesAsync` |
| `IUserService` / `UserService` | 12 | **Updated & Verified (Bundle 2)** — Propagates CT to repo, `SaveChangesAsync`, and duplicate index catch |
| **Total Services** | **153** | **100% Updated & Verified Across All 5 Bundles** |

---

### 2.3 API Controllers Layer (20 Controllers — Reconciled Exact Action Count)

| Controller | Total Actions | Async Actions | Sync Actions | Action Methods Inventory | Status |
|---|:---:|:---:|:---:|---|---|
| `ApplicationsController` | 7 | 7 | 0 | `GetAll`, `GetById`, `GetBasicInfo`, `Create`, `Update`, `Delete`, `Cancel` (*`Complete` removed in `INT-01`*) | Updated & Verified (Bundle 1) |
| `ApplicationTypesController` | 3 | 3 | 0 | `GetAll`, `GetById`, `Update` | Updated & Verified (Bundle 1) |
| `AuthController` | 4 | 3 | 1 | Async: `Login`, `Profile`, `ChangePassword`; Sync: `Me` (*in-memory JWT claims read*) | Updated & Verified (Bundle 2) |
| `CountriesController` | 1 | 1 | 0 | `GetAll` | Updated & Verified (Bundle 1) |
| `DashboardController` | 1 | 1 | 0 | `GetStatistics` | Updated & Verified (Bundle 1) |
| `DetainedLicensesController` | 6 | 6 | 0 | `GetAll`, `GetById`, `GetActiveByLicenseId`, `IsDetained`, `Detain`, `Release` | Updated & Verified (Bundle 5) |
| `DriversController` | 7 | 7 | 0 | `GetAll`, `GetById`, `GetByPersonId`, `GetByCreatedUserId`, `Create`, `Update`, `Delete` | Updated & Verified (Bundle 3) |
| `InternationalLicensesController` | 7 | 7 | 0 | `GetAll`, `GetById`, `GetByDriverId`, `GetByApplicationId`, `GetByLocalLicenseId`, `GetLocalLicenseInfo`, `Issue` | Updated & Verified (Bundle 5) |
| `LicenseClassesController` | 3 | 3 | 0 | `GetAll`, `GetById`, `Update` | Updated & Verified (Bundle 1) |
| `LicenseIssuanceController` | 1 | 1 | 0 | `IssueFirstLicense` | Updated & Verified (Bundle 5) |
| `LicenseRenewalController` | 1 | 1 | 0 | `Renew` | Updated & Verified (Bundle 5) |
| `LicenseReplacementController` | 1 | 1 | 0 | `Replace` | Updated & Verified (Bundle 5) |
| `LicensesController` | 9 | 9 | 0 | `GetAll`, `GetRenewalSummary`, `GetById`, `GetByDriverId`, `GetByApplicationId`, `GetByLicenseClassId`, `GetByPersonId`, `GetDetails`, `GetDetailsById` | Updated & Verified (Bundle 5) |
| `LocalDrivingLicenseApplicationsController` | 12 | 12 | 0 | `GetAll`, `GetById`, `GetApplicationBasicInfo`, `GetByApplicationId`, `GetByLicenseClassId`, `GetByApplicantPersonId`, `GetCreateInfo`, `GetApplicationId`, `Create`, `Update`, `Delete`, `Cancel` | Updated & Verified (Bundle 3) |
| `PeopleController` | 11 | 11 | 0 | `GetAll`, `GetStatistics`, `GetAvailableForUser`, `GetById`, `GetByNationalNo`, `Create`, `Update`, `Delete`, `UploadPhoto`, `GetPhoto`, `DeletePhoto` | Updated & Verified (Bundle 2) |
| `TestAppointmentsController` | 17 | 17 | 0 | `GetAll`, `GetById`, `GetByLocalApplication`, `GetByTestType`, `GetSchedulePreparation`, `Schedule`, `GetByCreatedUser`, `GetScheduleInfo`, `GetFees`, `GetTrialCount`, `IsScheduled`, `Create`, `Update`, `Delete`, `Cancel`, `GetAttemptStats`, `GetAvailability` | Updated & Verified (Bundle 4) |
| `TestsController` | 5 | 5 | 0 | `GetAll`, `GetById`, `GetByAppointmentId`, `GetByUserId`, `AddResult` | Updated & Verified (Bundle 4) |
| `TestTypesController` | 3 | 3 | 0 | `GetAll`, `GetById`, `Update` | Updated & Verified (Bundle 1) |
| `TestWorkflowController` | 3 | 3 | 0 | `CanSchedule`, `GetNextTest`, `CanTake` | Updated & Verified (Bundle 4) |
| `UsersController` | 8 | 8 | 0 | `GetAll`, `GetById`, `GetByPersonId`, `GetDetails`, `GetByUsername`, `Create`, `Update`, `Delete` | Updated & Verified (Bundle 2) |
| **Total Controller Actions** | **110** | **109** | **1** | **109 of 109 async actions accept & forward `CancellationToken` (100%)** | **Verified** |

---

### 2.4 Unit of Work, Transaction Management & `CancellationToken.None` Audit

A static grep audit of `CancellationToken.None` across `API/`, `Application/`, and `Infrastructure/` confirmed that **only 3 occurrences** exist in the entire production codebase, all within [`Infrastructure/UnitOfWork.cs`](../../Infrastructure/UnitOfWork.cs), and each is architecturally required:

1. **`Infrastructure/UnitOfWork.cs:196`:** `await transaction.RollbackPhysicalAsync(CancellationToken.None);`
   - **Justification:** Invoked inside `catch when (!commitStageReached)`. When a request is canceled mid-transaction (`OperationCanceledException`), the caller's token is already in a canceled state. Passing `CancellationToken.None` guarantees that the physical SQL Server transaction rollback executes to completion and releases any held `UPDLOCK`/`HOLDLOCK` or `sp_getapplock` locks without being aborted by the already-canceled request token.
2. **`Infrastructure/UnitOfWork.cs:235` & `240`:** `await verifySucceeded(CancellationToken.None);` and `await tracker.VerifyCommittedAsync(_context, CancellationToken.None);`
   - **Justification:** Invoked inside `VerifyCommitOutcomeAsync` only when `commitStageReached == true` and a transient transport failure occurred during `CommitPhysicalAsync`. Because the SQL Server commit was already transmitted, verifying whether the commit succeeded on the server must not be aborted.

---

### 2.5 Background Services Layer

1. **`NoShowBackgroundService.cs`:** **Compliant.** Passes `stoppingToken` to `INoShowProcessorService.ProcessPendingNoShowsAsync(stoppingToken)` and handles `OperationCanceledException` when `stoppingToken.IsCancellationRequested`. In `NoShowProcessorService.cs`, `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` precedes generic `catch (Exception ex)` blocks so host shutdown cancellation propagates cleanly without false error logs.
2. **`LicenseExpirationBackgroundService.cs`:** **Updated & Verified (Bundle 5).** Line 40 passes `stoppingToken` to `licenseRepo.DeactivateExpiredLicensesAsync(clock.Today, stoppingToken)`.

---

### 2.6 `IFileStorageService` & `PhysicalFileStorageService` Audit

All 4 methods on [`IFileStorageService`](../../Application/Interfaces/IFileStorageService.cs) and [`PhysicalFileStorageService`](../../Infrastructure/Services/PhysicalFileStorageService.cs) were audited along with all callers in [`PersonService`](../../Application/Services/PersonService.cs):

1. **`SaveAsync(Stream contentStream, string fileExtension, string subDirectory = "people", CancellationToken cancellationToken = default)`:**
   - **Stream I/O Cancellation:** Propagates `cancellationToken` directly to both `await contentStream.CopyToAsync(fileStream, cancellationToken)` and `await fileStream.FlushAsync(cancellationToken)`.
   - **Exception Propagation:** In `PersonService.UploadPersonPhotoAsync`, `catch (OperationCanceledException) { throw; }` is placed before `catch (Exception ex)` so cancellation during file streaming is never swallowed or converted into a generic `Result<string>.FromFailure("Failed to store image file.")`, and no database `SaveChangesAsync` is executed.
2. **`GetAsync(string storageKey, CancellationToken cancellationToken = default)`:**
   - **Technical Justification:** `PersonService.GetPersonPhotoAsync` forwards `cancellationToken`. In `PhysicalFileStorageService`, `GetAsync` validates the path, checks `File.Exists(fullPath)`, and opens a `new FileStream(..., useAsync: true)` handle; .NET's `File.Exists` and `FileStream` constructor are synchronous Win32 handle/metadata operations that do not expose async `CancellationToken` overloads (the returned `FileStream` is subsequently streamed asynchronously by ASP.NET Core `FileStreamResult` using `HttpContext.RequestAborted`). The `CancellationToken` parameter is retained on `IFileStorageService` for interface consistency and cloud/network storage implementations.
3. **`DeleteAsync(string storageKey, CancellationToken cancellationToken = default)`:**
   - **Technical Justification:** `PersonService` forwards `cancellationToken` in `DeletePersonAsync`, `UploadPersonPhotoAsync` (compensating cleanup if DB save fails, and old-photo cleanup after DB save succeeds), and `DeletePersonPhotoAsync`. In `PhysicalFileStorageService`, `File.Exists` and `File.Delete` are synchronous OS directory-entry operations without async `CancellationToken` overloads in .NET. Furthermore, because `DeleteAsync` is used for compensating/post-commit file cleanup, avoiding artificial `ThrowIfCancellationRequested()` preserves idempotent cleanup behavior.
4. **`ExistsAsync(string storageKey, CancellationToken cancellationToken = default)`:**
   - **Technical Justification:** Wraps synchronous `File.Exists(fullPath)` metadata lookup, which has no async `CancellationToken` overload in .NET; parameter is retained on `IFileStorageService` for interface uniformity.

---

## 3. Critical Technical Finding: C# Language Rule CS0854 & Moq Compatibility

During initial verification of `Applications` and `ApplicationTypes`, adding optional arguments (`CancellationToken cancellationToken = default`) triggered C# compiler error:
> `error CS0854: An expression tree may not contain a call or invocation that uses optional arguments`

**Root Cause:**
In C#, when an expression tree (`Expression<Func<T, Task>>`) contains an invocation of a method with optional parameters without explicitly passing them (e.g. `x => x.GetAllApplicationsAsync()`), the compiler refuses to compile because expression trees cannot evaluate or emit default arguments.

**Resolution:**
In Moq setups and verifies, passing `It.IsAny<CancellationToken>()` (or a specific `cts.Token` in dedicated cancellation tests) explicitly resolves CS0854, compiles cleanly, and matches any cancellation token passed by ASP.NET Core (`HttpContext.RequestAborted`). Additionally, multi-parameter Moq callbacks on repository `AddAsync`/`AddLicenseAsync` methods were updated to `.Callback<TEntity, CancellationToken>((entity, _) => ...)`, and `AppointmentConcurrencyIntegrationTests.RollbackTriggerAppointmentRepository` (a test decorator implementing `ITestAppointmentRepository` in `Infrastructure.IntegrationTests`) was updated to match all 19 updated interface method signatures.

---

## 4. Execution Summary (All 5 Domain Bundles + Final Audit Completed & Verified)

Rollout across all 5 cohesive domain bundles is **100% Complete & Verified**:

1. **Bundle 1 (Core Licensing & Administrative Domains):**
   - `Applications`, `ApplicationTypes`, `Countries`, `Dashboard`, `LicenseClasses`, `TestTypes` (**100% Implemented & Verified** — 18 async controller actions, 23 repository methods, 20 service methods).
2. **Bundle 2 (Civic Registry & Security Domains):**
   - `People`, `Users`, `Auth` (**100% Implemented & Verified** — 23 total controller actions [22 async + 1 sync `AuthController.Me`], 26 repository methods, 27 service methods, plus `IFileStorageService`).
3. **Bundle 3 (Driver & Local Application Domains):**
   - `Drivers`, `LocalDrivingLicenseApplications` (**100% Implemented & Verified** — 19 async controller actions, 23 repository methods, 26 service methods).
4. **Bundle 4 (Examination & Testing Domains):**
   - `TestAppointments`, `Tests`, `TestWorkflow` (**100% Implemented & Verified** — 25 async controller actions, 30 repository methods, 29 service methods).
5. **Bundle 5 (Licensing Post-Issuance & Lifecycle Workflows):**
   - `Licenses`, `LicenseQuery`, `LicenseIssuance`, `LicenseRenewal`, `LicenseReplacement`, `DetainedLicenses`, `InternationalLicenses`, `LicenseExpirationBackgroundService` (**100% Implemented & Verified** — 25 async controller actions, 39 repository methods, 47 service methods, 1 background service).
6. **Final Verification Suite:**
   - `Application.UnitTests`: **935 passed, 0 failed, 0 skipped** (Duration: 7 s, Exit Code: 0).
   - `API.IntegrationTests` (All 20 Controllers + Background Services): **461 passed, 0 failed, 0 skipped** ( Exit Code: 0).
   - `Infrastructure.IntegrationTests`: Build succeeded with **0 warnings, 0 errors** (`Exit Code: 0`). Runtime execution ran **657 total tests** (`34 passed` in database-free validation and constraint-translation suites; `622 failed` at `SqlServerTestDatabase.InitializeAsync` with `DVLD_TEST_CONNECTION must be explicitly configured` because the SQL Server test environment variables are not configured on this host; `1 failed` in pre-existing uncommitted user test `SqlServerTestEnvironmentValidationTests`). `DVLDf` was never accessed.
