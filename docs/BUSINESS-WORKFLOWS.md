# DVLD Business Workflows

This document describes business workflows supported by the current Application, Infrastructure, API, and test implementation.

## 1. Local driving-license workflow

```text
Person
  ↓
Application
  ↓
LocalDrivingLicenseApplication
  ↓
Theory test
  ↓
Written test
  ↓
Practical test
  ↓
All required tests passed
  ↓
First-license issuance
```

The current required test sequence is:

1. Theory
2. Written
3. Practical

The backend remains authoritative even when the WPF client disables unavailable actions.

## 2. Test scheduling

Test scheduling is a business workflow rather than an unrestricted CRUD insert.

The scheduling area tracks:

- local application
- test type
- appointment date
- trial number
- fees
- retake application
- test result state
- appointment lock state

The service exposes preparation information, fees, trial count, and whether an appointment is already scheduled.

The workflow checks the application's current test state and required test order.

## 3. Test-result workflow

Recording a test result is concurrency-sensitive.

```text
Authenticated user
      ↓
Serializable transaction
      ↓
Retrieve appointment for protected operation
      ↓
Appointment exists?
      ↓
Appointment locked?
      ↓
Workflow eligibility valid?
      ↓
Duplicate result?
      ↓
Create Test
      ↓
Lock appointment
      ↓
SaveChanges
      ↓
Commit
```

The operation rejects invalid states such as:

- missing appointment
- locked appointment
- invalid workflow order/state
- duplicate result

The result model supports:

```text
NotTaken
Pass
Fail
```

## 4. First-license issuance

The first-license workflow coordinates multiple records transactionally.

```text
Validate local application
        ↓
Authenticated user
        ↓
Application type is New
        ↓
Re-check application state
        ↓
All required tests passed
        ↓
No existing license for application
        ↓
Find or create Driver
        ↓
No active license for Driver + LicenseClass
        ↓
Create License
        ↓
SaveChanges
        ↓
Complete Application
        ↓
Commit
```

Concurrent attempts are protected through transaction/concurrency controls and database uniqueness constraints.

## 5. License renewal

Renewal requires an eligible existing license.

The workflow verifies conditions including:

- source license exists
- source license is active
- source license is expired
- required driver/license-class information exists

Then:

```text
Create renewal Application
        ↓
Deactivate old License
        ↓
Create new License
        ↓
Apply license-class validity/fees
        ↓
SaveChanges
        ↓
Complete Application
        ↓
Commit
```

The operation is transactional.

## 6. License replacement

Replacement supports:

- Lost
- Damaged

The corresponding application types are:

| Reason | Application Type |
|---|---:|
| Lost | 3 |
| Damaged | 4 |

The source license must be active and not expired.

The old license is deactivated and a replacement is created.

The replacement keeps the old license expiration date rather than starting a new validity period.

The operation is transactional.

## 7. License detention

Detention validates that the source license is eligible for detention.

The operation creates a detention record containing license, fine, date, and creator information and deactivates the license within the same transaction.

A filtered unique database index prevents more than one unreleased detention for the same license.

## 8. Release of detained license

Release starts from the detention row.

```text
Load detention for update
       ↓
Already released?
       ↓
Reject if already released
       ↓
Create release Application
       ↓
Mark detention as released
       ↓
Evaluate license reactivation conditions
       ↓
SaveChanges
       ↓
Complete Application
       ↓
Commit
```

The license is reactivated only when the implementation's conditions allow it, including checking for another active license for the same driver/class and checking expiration.

The workflow is transactional.

## 9. International license issuance

The current workflow requires:

- source local license exists
- source license is active
- source license is not expired
- source license belongs to the required class
- driver exists
- no conflicting active international license exists
- source local license has not already been used for a conflicting international license

The current required class is **Class 3**.

The workflow creates:

- application type **6**
- international license

The international license validity is one year.

Creation uses a Serializable transaction.

Database constraints reinforce the relevant uniqueness rules.

## 10. Application completion and cancellation

Applications expose explicit completion and cancellation operations.

The Application service remains responsible for deciding whether a status transition is valid.

## 11. Business rules vs UI behavior

Client applications (`Presentation (WPF)` and `ClientApp`) may disable or hide actions according to:

- role
- workflow state
- test state
- license state

These are usability behaviors.

The Application services and API authorization policies are the authoritative enforcement mechanisms.

## 12. Transaction and concurrency philosophy

The project treats multi-record workflows as business transactions rather than unrelated CRUD operations.

Concurrency-sensitive workflows use transaction isolation (`IsolationLevel.Serializable`), `UnitOfWork.ExecuteInTransactionAsync` (`Infrastructure/UnitOfWork.cs`), and database uniqueness together:

- **Nested-transaction guard:** `EnsureNoActiveTransaction()` prevents accidental nested transactions on the same `UnitOfWork`.
- **Deferred physical commit:** Calling `transaction.CommitAsync()` inside `ExecuteInTransactionAsync` flushes pending tracked changes (`SaveChangesAsync`) and records the commit intent, while deferring the physical SQL commit until the workflow delegate returns a successful result.
- **Rollback and state reset on failure `Result`:** If a multi-step workflow returns `Result.Failure` (or throws an exception) after intermediate saves, `UnitOfWork` rolls back the database transaction and clears `DVLDDbContext.ChangeTracker`.
- **Ambiguous-commit verification:** If a transient disconnect occurs while committing under EF Core's `EnableRetryOnFailure()` strategy, `UnitOfWork` verifies whether the commit succeeded on SQL Server using `TransactionVerificationTracker` (`SavingChanges`/`SavedChanges` probes) before allowing a retry, preventing duplicate execution of already-committed operations. Verification occurs only when `tracker.CanVerify` allows it (the tracker recorded added, modified, or deleted entity probes) or an explicit `verifySucceeded` callback was supplied; if neither is available, `UnitOfWork` throws an `InvalidOperationException` instead of retrying. Production workflows in `Application/Services` call the three-parameter `ExecuteInTransactionAsync(operation, isolationLevel, cancellationToken)` overload and rely on this tracker (when `tracker.CanVerify` permits verification); none of them pass an explicit `verifySucceeded` callback. `IUnitOfWork` also exposes a four-parameter overload accepting an optional `verifySucceeded` delegate, which is exercised separately in `Test/Infrastructure.IntegrationTests/UnitOfWorkTests.cs`.

Examples include:

- test-result recording
- first-license issuance
- renewal
- replacement
- detention
- detention release
- international-license issuance
- test appointment workflows
- local application workflows

## 13. Responsibility boundaries

| Concern | Owner |
|---|---|
| HTTP request/response | API |
| Authentication/authorization | API |
| Business workflow | Application |
| Core concepts | Domain |
| Database persistence | Infrastructure |
| Transaction coordination | Unit of Work / workflow |
| Database invariants | SQL Server / EF Core |
| UI usability | Presentation |
