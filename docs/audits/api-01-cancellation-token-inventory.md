# API-01 — Comprehensive CancellationToken Inventory & Architectural Assessment

> **Task ID:** `API-01`  
> **Classification:** Confirmed Technical Remediation  
> **Priority:** P2  
> **Phase:** 12.3 (Reliability, Observability & Core Policy)  
> **Date:** 2026-11-09  
> **Governance Authority:** [`AGENTS.md`](../../AGENTS.md) / [`ENGINEERING-ROADMAP.md`](../../ENGINEERING-ROADMAP.md)  

---

## 1. Architectural Objective & Problem Statement

Client HTTP requests can be aborted at any time (e.g., user navigates away, network timeout, client drops connection). In ASP.NET Core, the framework signals client disconnection via `HttpContext.RequestAborted` (`CancellationToken`).

Prior to `API-01`:
1. All 111 Controller action methods across 20 API controllers omitted `CancellationToken cancellationToken`.
2. 151 out of 153 Application Service async methods omitted `CancellationToken`, or dropped it before calling repositories.
3. 139 out of 141 Repository async methods and their internal EF Core queries (`ToListAsync()`, `FirstOrDefaultAsync()`, `SingleOrDefaultAsync()`, `AnyAsync()`, `CountAsync()`, `AddAsync()`) omitted `cancellationToken`, causing orphaned SQL queries to continue executing on SQL Server even after client disconnection.
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
| `IDashboardRepository` / `DashboardRepository` | 1 | 1 | `CountAsync` |
| `IDetainedLicenseRepository` / `DetainedLicenseRepository` | 7 | 7 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `IDriverRepository` / `DriverRepository` | 11 | 11 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `IInternationalRepository` / `InternationalRepository` | 8 | 8 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `ILicenseClassRepository` / `LicenseClassRepository` | 3 | 3 | `ToListAsync`, `FirstOrDefaultAsync` |
| `ILicenseRepository` / `LicenseRepository` | 24 | 24 | `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `ExecuteSqlInterpolatedAsync`, `AddAsync` |
| `ILocalDrivingLicenseApplicationRepository` / `LocalDrivingLicenseApplicationRepository` | 12 | 12 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `IPersonRepository` / `PersonRepository` | 13 | 13 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| `ITestAppointmentRepository` / `TestAppointmentRepository` | 24 | 22 | `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync`, `CountAsync`, `AddAsync` (*2 locking methods already had CT*) |
| `ITestRepository` / `TestRepository` | 6 | 6 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`, `AddAsync` |
| `ITestTypeRepository` / `TestTypeRepository` | 3 | 3 | `ToListAsync`, `FirstOrDefaultAsync` |
| `IUserRepository` / `UserRepository` | 13 | 13 | `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AddAsync` |
| **Total Repositories** | **141** | **139** | **Full EF Core Query Stack** |

---

### 2.2 Application Services Layer (26 Service Interfaces & Implementations)

| Service Interface / Implementation | Total Async Methods | Status & Breakage Identified |
|---|:---:|---|
| `IApplicationService` / `ApplicationService` | 9 | Omitted; `SaveAsync` dropped token |
| `IApplicationTypeService` / `ApplicationTypeService` | 3 | Omitted |
| `IAuthService` / `AuthService` | 1 | Omitted |
| `ICountryService` / `CountryService` | 1 | Omitted |
| `ICurrentUserService` / `CurrentUserService` | 0 | Synchronous claims reader |
| `IDashboardService` / `DashboardService` | 1 | Omitted |
| `IDetainedLicenseService` / `DetainedLicenseService` | 6 | Omitted; transactions drop token |
| `IDriverService` / `DriverService` | 11 | Omitted |
| `IFileStorageService` / `PhysicalFileStorageService` | 4 | Stream I/O methods omit `CancellationToken` |
| `IImageProcessingService` / `ImageProcessingService` | 0 | Pure CPU/in-memory image manipulation |
| `IInternationalService` / `InternationalService` | 8 | Omitted; transactions drop token |
| `IJwtTokenService` / `JwtTokenService` | 0 | Synchronous in-memory token generation |
| `ILicenseClassService` / `LicenseClassService` | 3 | Omitted |
| `ILicenseIssuanceService` / `LicenseIssuanceService` | 1 | Omitted; transaction drops token |
| `ILicenseQueryService` / `LicenseQueryService` | 15 | Omitted; batched queries drop token |
| `ILicenseRenewalService` / `LicenseRenewalService` | 1 | Omitted; transaction drops token |
| `ILicenseReplacementService` / `LicenseReplacementService` | 1 | Omitted; transaction drops token |
| `ILicenseService` / `LicenseService` | 15 | Omitted |
| `ILocalDrivingLicenseApplicationService` / `LocalDrivingLicenseApplicationService` | 15 | Omitted |
| `INoShowProcessorService` / `NoShowProcessorService` | 2 | **Compliant**; propagates CT to UoW & Repos |
| `IPersonService` / `PersonService` | 14 | Omitted |
| `ITestAppointmentService` / `TestAppointmentService` | 17 | Omitted |
| `ITestService` / `TestService` | 5 | Omitted |
| `ITestTypeService` / `TestTypeService` | 3 | Omitted |
| `ITestWorkflowService` / `TestWorkflowService` | 5 | Omitted |
| `IUserService` / `UserService` | 12 | Omitted; duplicate index catch drops token |
| **Total Services** | **153** | **151 async methods lack Token** |

---

### 2.3 API Controllers Layer (20 Controllers)

| Controller | Actions Count | Status |
|---|:---:|---|
| `ApplicationsController` | 7 | Updated & Verified (Bundle 1) |
| `ApplicationTypesController` | 3 | Updated & Verified (Bundle 1) |
| `AuthController` | 4 | Pending (Bundle 2) |
| `CountriesController` | 1 | Updated & Verified (Bundle 1) |
| `DashboardController` | 1 | Updated & Verified (Bundle 1) |
| `DetainedLicensesController` | 6 | Pending (Bundle 5) |
| `DriversController` | 7 | Pending (Bundle 3) |
| `InternationalLicensesController` | 7 | Pending (Bundle 5) |
| `LicenseClassesController` | 3 | Updated & Verified (Bundle 1) |
| `LicenseIssuanceController` | 1 | Pending (Bundle 5) |
| `LicenseRenewalController` | 1 | Pending (Bundle 5) |
| `LicenseReplacementController` | 1 | Pending (Bundle 5) |
| `LicensesController` | 9 | Pending (Bundle 5) |
| `LocalDrivingLicenseApplicationsController` | 12 | Pending (Bundle 3) |
| `PeopleController` | 11 | Pending (Bundle 2) |
| `TestAppointmentsController` | 18 | Pending (Bundle 4) |
| `TestsController` | 5 | Pending (Bundle 4) |
| `TestTypesController` | 3 | Updated & Verified (Bundle 1) |
| `TestWorkflowController` | 3 | Pending (Bundle 4) |
| `UsersController` | 8 | Pending (Bundle 2) |
| **Total Controller Actions** | **111** | **18 updated (Bundle 1 Complete); 93 pending** |

---

### 2.4 Unit of Work & Transaction Management

- **`IUnitOfWork` & `UnitOfWork`:** Already fully equipped with `CancellationToken cancellationToken = default` across `SaveChangesAsync`, `BeginTransactionAsync`, and `ExecuteInTransactionAsync`.
- **Physical Rollback Safety:** In `UnitOfWork.cs:196`, during exceptional catch blocks, `transaction.RollbackPhysicalAsync(CancellationToken.None)` is correctly invoked with `CancellationToken.None` so that transaction abort does not fail if the caller's cancellation token has already fired.

---

### 2.5 Background Services Layer

1. **`NoShowBackgroundService.cs`:** **Compliant.** Passes `stoppingToken` to `INoShowProcessorService.ProcessPendingNoShowsAsync(stoppingToken)` and handles `OperationCanceledException`.
2. **`LicenseExpirationBackgroundService.cs`:** **Breakage Point.** Line 40 calls `licenseRepo.DeactivateExpiredLicensesAsync(clock.Today)` without passing `stoppingToken`. Must be updated to pass `stoppingToken`.

---

## 3. Critical Technical Finding: C# Language Rule CS0854 & Moq Compatibility

During initial verification of `Applications` and `ApplicationTypes`, adding optional arguments (`CancellationToken cancellationToken = default`) triggered C# compiler error:
> `error CS0854: An expression tree may not contain a call or invocation that uses optional arguments`

**Root Cause:**
In C#, when an expression tree (`Expression<Func<T, Task>>`) contains an invocation of a method with optional parameters without explicitly passing them (e.g. `x => x.GetAllApplicationsAsync()`), the compiler refuses to compile because expression trees cannot evaluate or emit default arguments.

**Resolution:**
In Moq setups and verifies, passing `It.IsAny<CancellationToken>()` (or `CancellationToken.None` where appropriate) explicitly resolves CS0854, compiles cleanly, and matches any cancellation token passed by ASP.NET Core (`HttpContext.RequestAborted`).

---

## 4. Execution Plan (Grouped Vertical Slices)

To avoid cross-cutting compile failures and ensure steady verification, rollout proceeds across 5 cohesive domain bundles:

1. **Bundle 1 (Core Licensing & Administrative Domains):**
   - `Applications`, `ApplicationTypes`, `Countries`, `Dashboard`, `LicenseClasses`, `TestTypes` (**100% Implemented & Verified** - 18 controller actions, 23 repository methods, 20 service methods).
2. **Bundle 2 (Civic Registry & Security Domains):**
   - `People`, `Users`, `Auth` (*Next*).
3. **Bundle 3 (Driver & Local Application Domains):**
   - `Drivers`, `LocalDrivingLicenseApplications`.
4. **Bundle 4 (Examination & Testing Domains):**
   - `TestAppointments`, `Tests`, `TestWorkflow`.
5. **Bundle 5 (Licensing Post-Issuance & Lifecycle Workflows):**
   - `Licenses`, `LicenseQuery`, `LicenseIssuance`, `LicenseRenewal`, `LicenseReplacement`, `DetainedLicenses`, `InternationalLicenses`.
   - `LicenseExpirationBackgroundService`.
