# DVLD Database Documentation

## 1. Database technology

- SQL Server
- Entity Framework Core 8
- EF Core model snapshot: 8.0.18

Runtime access uses a scoped `DVLDDbContext`.

## 2. Entities / tables

- Applications
- ApplicationTypes
- Countries
- DetainedLicenses
- Drivers
- InternationalLicenses
- Licenses
- LicenseClasses
- LocalDrivingLicenseApplications
- People
- Tests
- TestAppointments
- TestTypes
- Users

## 3. Important integrity constraints

| Entity | Constraint |
|---|---|
| People | Unique NationalNo |
| Users | Unique UserName; unique PersonId |
| Drivers | Unique PersonId |
| LocalDrivingLicenseApplications | Unique ApplicationID |
| Tests | Unique TestAppointmentID |
| Licenses | Unique ApplicationID; unique active Driver + LicenseClass |
| InternationalLicenses | Unique source local license; unique active Driver |
| DetainedLicenses | Unique unreleased detention per License |

Important foreign keys use restrictive delete behavior.

## 4. Indexes

### Applications

Indexes include:

- ApplicantPersonID
- ApplicationTypeID
- CreatedByUserID

### Countries

CountryName is unique.

### Drivers

CreatedByUserID is indexed; PersonID is unique.

### LocalDrivingLicenseApplications

LicenseClassID is indexed.

ApplicationID uses the named unique index:

`UX_LocalDrivingLicenseApplications_ApplicationID`

### TestAppointments

Important indexes include:

- RetakeTestApplicationID
- TestTypeID
- CreatedByUserID + AppointmentDate
- LocalDrivingLicenseApplicationID + AppointmentDate
- LocalDrivingLicenseApplicationID + TestTypeID

### Tests

CreatedByUserID is indexed. TestAppointmentID is unique.

### Licenses

CreatedByUserID and LicenseClass are indexed.

A filtered unique index protects:

`(DriverID, LicenseClass) WHERE IsActive = 1`

### InternationalLicenses

ApplicationID and CreatedByUserID are indexed.

A filtered unique index protects:

`DriverID WHERE IsActive = 1`

IssuedUsingLocalLicenseID is unique.

### DetainedLicenses

ReleaseApplicationID is indexed.

A filtered unique index protects:

`(LicenseID, IsReleased) WHERE IsReleased = 0`

## 5. Filtered unique indexes

The following database constraints reinforce state-dependent business rules:

```text
Licenses:
(DriverID, LicenseClass) WHERE IsActive = 1

InternationalLicenses:
DriverID WHERE IsActive = 1

DetainedLicenses:
(LicenseID, IsReleased) WHERE IsReleased = 0
```

They complement Application validation and transaction/concurrency controls.

## 6. Property constraints

### Person

- NationalNo: required, max 20, unique
- FirstName: required, max 50
- SecondName: required, max 50
- ThirdName: optional, max 50
- LastName: required, max 50
- DateOfBirth: required
- Gender: required
- Address: required, max 200
- Phone: required, max 20
- Email: optional, max 100
- ImagePath: optional, max 500
- NationalityCountryID: indexed

### User

- UserName: required, max 50, unique
- Password: required, max 200
- IsActive: required
- Role: integer with Staff model default
- PersonId: unique
- Person relationship uses restrictive delete behavior

### License

- Notes: max 500
- IssueReason: byte
- PaidFees: decimal(18,2)

### Test

- TestResult: required
- Notes: max 500
- CreatedByUserID: required

### TestAppointment

- AppointmentDate: required
- TestType, LocalDrivingLicenseApplication, User, and optional RetakeTestApplication are foreign-key relationships
- Relationships use restrictive delete behavior

## 7. Money precision

Configured monetary fields use `decimal(18,2)`:

- Application.PaidFees
- ApplicationType.ApplicationFees
- DetainedLicense.FineFees
- LicenseClass.ClassFees
- License.PaidFees
- TestAppointment.PaidFees
- TestType.TestTypeFees

## 8. Migrations

Current migrations:

```text
20260901000000_InitialCreate
20260903234022_AddUniqueLocalApplicationApplicationId
20260906111058_AddLicenseInternationalAndDetainedConstraints
20260907233340_AddUserRole
```

### AddUniqueLocalApplicationApplicationId

Adds:

`UX_LocalDrivingLicenseApplications_ApplicationID`

### AddLicenseInternationalAndDetainedConstraints

Adds important TestAppointment composite indexes and filtered uniqueness for active international licenses and unreleased detentions.

### AddUserRole

Adds `Users.Role` with the model's Staff default.

## 9. Runtime vs design-time DbContext

Runtime:

```text
AddDbContext<DVLDDbContext>(...)
```

Design time:

```text
DVLDDbContextFactory : IDesignTimeDbContextFactory<DVLDDbContext>
```

The design-time factory exists for EF Core tooling/migrations and is not the runtime DI path.

## 10. Integration-test database

The test fixture obtains its base connection from:

`DVLD_TEST_CONNECTION`

Test databases are isolated per fixture/factory and removed afterward. Migrations are applied, reachability is verified, and pending migration state is checked.

## 11. Schema change checklist

1. Change entity/configuration.
2. Generate and review migration.
3. Add/update tests.
4. Verify indexes and constraints.
5. Update documentation.
6. Run integration tests.
7. Run the complete test suite.
