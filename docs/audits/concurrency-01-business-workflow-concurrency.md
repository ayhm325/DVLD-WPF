# CONCURRENCY-01 — Business Workflow Concurrency & Transaction Safety

> **Date:** 2026-11-09
> **Scope:** Critical workflow concurrency audit, UnitOfWork/DbContext boundary verification, and the P0 last-administrator race fix.
> **WPF:** untouched. **`DVLDf`:** never accessed.

---

## 1. Result

- **DbContext/UnitOfWork boundaries: VERIFIED.** Repositories inject the scoped `DVLDDbContext`; there is no `IDbContextFactory` (or `AddDbContextFactory`) anywhere in production code. All repositories, the UnitOfWork, and transactions share one context/connection per request.
- **P0 last-active-administrator race: FIXED and verified on SQL Server** with a cross-process application lock (`sp_getapplock`, transaction-owned) and an authoritative count inside the mutation transaction.
- Other high-risk workflows were inspected; their existing protections (Serializable transactions, filtered unique indexes, `UPDLOCK`/`HOLDLOCK`, appointment `sp_getapplock`) were preserved without changes.

## 2. Workflow risk matrix

| Workflow | Invariant | Competing operations | Existing protection | Status |
|---|---|---|---|---|
| First license issuance | One active license per driver/class; application completes once | Two issuance requests | `Serializable` transaction; filtered unique index `(DriverID, LicenseClass) WHERE IsActive=1`; application state checked inside transaction | Protected (not re-tested here) |
| License renewal | Old license deactivated exactly once; one replacement | Concurrent renewals | `Serializable`; `GetForUpdateAsync` (`UPDLOCK, HOLDLOCK`); filtered unique index; application-status check | Protected |
| License replacement | One replacement per application | Concurrent replacements | `Serializable`; `UPDLOCK, HOLDLOCK` on license/application | Protected |
| International license | One active international license per driver | Concurrent issuance | `Serializable`; filtered unique index on `DriverID WHERE IsActive=1` | Protected |
| Detention / release | One active detention per license; release once | Concurrent detain/release | `Serializable`; `UPDLOCK, HOLDLOCK`; filtered unique index `WHERE IsReleased=0` | Protected |
| Test appointment booking | No duplicate active appointment; slot uniqueness | Concurrent bookings | `sp_getapplock` (applicant + slot), `UPDLOCK` on local application, Serializable checks | Protected |
| Test result submission | One result per appointment; correct progression | Concurrent submissions | `Serializable`; appointment `UPDLOCK`; test uniqueness; workflow checks | Protected |
| Application create/cancel/complete | Status transitions are legal and single-shot | Concurrent transitions | `UPDLOCK` reads; state checks; conflicts mapped to 409 | Protected |
| **Administrator role/activation/delete** | **At least one active administrator always exists** | **Two admins demote/deactivate/delete each other concurrently** | **Before: check-then-act with no transaction/lock (P0). After: `sp_getapplock('DVLD_AdminMutation')` + authoritative count inside the transaction** | **FIXED** |
| User create/update (non-security) | Unique username/person; last-admin rule | Concurrent updates | Unique indexes; duplicate exceptions translated to 409 | Protected |
| Reference data (types/classes/countries) | Unique names | Concurrent edits | Unique indexes; last-writer-wins for fee/description edits | Acceptable |

## 3. DbContext and UnitOfWork verification

- `Program.cs` registers `AddDbContext<DVLDDbContext>` (scoped) and repositories/`IUnitOfWork` as scoped.
- Every repository constructor receives `DVLDDbContext`; no repository constructs its own context.
- `UnitOfWork` begins/commits on the same `_context.Database` connection.
- Integration tests construct independent contexts explicitly; production never does.
- **Conclusion:** no split-context transaction defect currently exists.

## 4. P0 defect and fix

### Defect

`UserService.UpdateUserAsync`/`DeleteUserAsync` counted active administrators, then saved outside any transaction or lock. Two concurrent demotions could both read `2`, both pass, and both commit, leaving **zero** active administrators (system lockout).

### Fix

- Added `IUserRepository.AcquireAdminMutationLockAsync`, which executes `sp_getapplock` with `@Resource='DVLD_AdminMutation'`, `@LockMode='Exclusive'`, `@LockOwner='Transaction'`, 5-second timeout. It fails closed if no transaction is active.
- `UpdateUserAsync`: when the change removes an active admin, the mutation, authoritative `GetActiveAdminCountAsync`, session revocation and `SaveChanges` run inside one `ExecuteInTransactionAsync` callback after acquiring the lock. A loser sees the committed count of 1 and returns **409 Conflict**.
- `DeleteUserAsync`: admin deletion follows the same lock + count + delete + revoke + save transaction.
- The lock is released automatically on commit/rollback; it is database-scoped, so it protects across API processes, not just in-process.

### Why not optimistic concurrency?

The invariant is global ("at least one row in a predicate"), not a single-row version check. A rowversion on the target user would not detect that *another* administrator was demoted concurrently. A predicate/range lock or a global lock is required; the application lock is the smallest mechanism that also works across processes without schema changes.

## 5. SQL verification

`Test/Infrastructure.IntegrationTests/Concurrency/LastAdministratorConcurrencyTests.cs` (real SQL, isolated ephemeral DB, two independent contexts/UnitOfWork/transactions):

1. `TwoConcurrentAdminDemotions_LeaveAtLeastOneActiveAdministrator` — **PASS**: exactly one success, one **409 Conflict**, one active administrator among the pair; one demoted.
2. `ConcurrentAdminDemotion_WhenOnlyOneAdminRemains_RejectsWithoutMutation` — **PASS**: deactivating the last admin returns **409** and leaves one active.

## 6. Error mapping

- Losing concurrency operations return `ErrorType.Conflict` → HTTP **409** via the existing `ResultHttpMapper`.
- No database exception or SQL detail is exposed; unexpected persistence failures remain 500.

## 7. Regression

| Suite | Result |
|---|---|
| `Application.UnitTests` | **945 passed, 0 failed** |
| Database-free `API.IntegrationTests` | **514 passed, 0 failed** |
| Last-administrator SQL concurrency | **2 passed, 0 failed** |

## 8. Remaining risks / not implemented

- Workflows other than the administrator invariant were audited but not re-executed under new SQL race tests in this pass; their protections are existing and unchanged.
- `ApplicationService.UpdateApplicationAsync` can still return 500 for a true no-op update (`CAND-04`), documented but out of scope.
- Deadlock/retry behavior for the Serializable licensing transactions was not stress-tested.
- The application lock serializes only administrator security-state mutations; it does not affect other workflows.

## 9. Files

- `Application/Interfaces/IUserRepository.cs`
- `Infrastructure/Repositories/UserRepository.cs`
- `Application/Services/UserService.cs`
- `Test/Application.UnitTests/Services/UserServiceTests.cs`
- `Test/Infrastructure.IntegrationTests/Concurrency/LastAdministratorConcurrencyTests.cs`
