# DVLD Database Documentation

## Database

SQL Server + Entity Framework Core 8. The model snapshot reports EF Core 8.0.18.

## Tables

Applications, ApplicationTypes, Countries, DetainedLicenses, Drivers, InternationalLicenses, Licenses, LicenseClasses, LocalDrivingLicenseApplications, People, Tests, TestAppointments, TestTypes, Users.

## Relationship overview

```mermaid
erDiagram
    People ||--o{ Applications : applicant
    ApplicationTypes ||--o{ Applications : type
    Users ||--o{ Applications : created_by
    Applications ||--|| LocalDrivingLicenseApplications : local_application
    LicenseClasses ||--o{ LocalDrivingLicenseApplications : class
    LocalDrivingLicenseApplications ||--o{ TestAppointments : appointments
    TestTypes ||--o{ TestAppointments : type
    TestAppointments ||--o| Tests : result
    People ||--o| Drivers : driver
    Drivers ||--o{ Licenses : owns
    LicenseClasses ||--o{ Licenses : class
    Applications ||--o| Licenses : application
    Licenses ||--o{ DetainedLicenses : detention
    Drivers ||--o{ InternationalLicenses : international
    Licenses ||--o{ InternationalLicenses : source_license
```

## Keys and important indexes

- Applications: PK ApplicationID; indexes ApplicantPersonID, ApplicationTypeID, CreatedByUserID.
- ApplicationTypes: PK ApplicationTypeId.
- Countries: PK CountryId; unique CountryName.
- DetainedLicenses: PK DetainID; indexes CreatedByUserID, ReleaseApplicationID, ReleasedByUserID; filtered unique (LicenseID, IsReleased) where IsReleased = 0.
- Drivers: PK DriverID; unique PersonID; index CreatedByUserID.
- InternationalLicenses: PK InternationalLicenseID; indexes ApplicationID and CreatedByUserID; filtered unique DriverID where IsActive = 1; unique IssuedUsingLocalLicenseID.
- Licenses: PK LicenseID; unique ApplicationID; indexes CreatedByUserID and LicenseClass; filtered unique (DriverID, LicenseClass) where IsActive = 1.
- LicenseClasses: PK LicenseClassID.
- LocalDrivingLicenseApplications: PK LocalDrivingLicenseApplicationID; unique ApplicationID; index LicenseClassID.
- People: PK PersonId; unique NationalNo; index NationalityCountryID.
- Tests: PK TestID; unique TestAppointmentID; index CreatedByUserID.
- TestAppointments: PK TestAppointmentID; indexes RetakeTestApplicationID, TestTypeID, CreatedByUserID+AppointmentDate, LocalDrivingLicenseApplicationID+AppointmentDate, LocalDrivingLicenseApplicationID+TestTypeID.
- TestTypes: PK TestTypeId.
- Users: PK UserId; unique PersonId; unique UserName.

## Integrity

Foreign keys, restrictive delete behavior, required fields, unique indexes, filtered unique indexes, and decimal precision all contribute to integrity.

Money values use decimal(18,2) where configured.

## Schema change checklist

1. Update entity/configuration.
2. Create/update migration as appropriate.
3. Review migration.
4. Update tests.
5. Update this document.
6. Run integration tests.
