# DVLD Business Workflows

## Local license workflow

```text
Person
 ↓
Application
 ↓
LocalDrivingLicenseApplication
 ↓
Test scheduling
 ↓
Theory / Written / Practical
 ↓
Required tests passed
 ↓
License issuance
```

## Test result workflow

1. Require authenticated user.
2. Begin Serializable transaction.
3. Retrieve appointment.
4. Reject missing appointment.
5. Reject locked appointment.
6. Validate workflow eligibility.
7. Reject duplicate result.
8. Create Test.
9. Lock appointment.
10. SaveChanges.
11. Commit.
12. Roll back on failure.

Integration tests cover locked, future, invalid-order, duplicate, rollback, and concurrent cases.

## First-license issuance

Dedicated controller/service workflow coordinating license/application changes as a multi-step operation.

## Renewal

Dedicated controller/service. Request contains OldLicenseId and Notes.

## Replacement

Dedicated controller/service. Request contains OldLicenseId and ReplacementReason.

## Detention/release

Detention records LicenseId and FineFees. Release uses DetainId. The database prevents multiple unreleased detention records for one license.

## International license

Confirmed checks:

- Source license exists.
- Active.
- Not expired.
- Required class satisfied.
- Driver exists.
- No active international license already exists.

Application and international-license creation are performed in a Serializable transaction.

## Rule location

The backend is authoritative. UI disabling is for usability only; business rules must still be enforced by Application/Domain logic.
