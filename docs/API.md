# DVLD API Reference

Base route: `/api/[controller]`.

## Authorization policies

- `StaffOnly`
- `StaffOrAdmin`
- `AdminOnly`

## Auth

- POST `/api/Auth/login` — anonymous; login rate limited.
- GET `/api/Auth/me`
- GET `/api/Auth/profile`
- POST `/api/Auth/change-password`

## People — StaffOrAdmin

- GET `/api/People`
- GET `/api/People/{id}`
- GET `/api/People/national/{nationalNo}`
- POST `/api/People`
- PUT `/api/People/{id}`
- DELETE `/api/People/{id}`

## Applications — StaffOnly

- GET `/api/Applications`
- GET `/api/Applications/{id}`
- GET `/api/Applications/{id}/basic-info`
- POST `/api/Applications`
- PUT `/api/Applications/{id}`
- DELETE `/api/Applications/{id}`
- POST `/api/Applications/{id}/complete`
- POST `/api/Applications/{id}/cancel`

## Local Driving License Applications — StaffOnly

- GET `/api/LocalDrivingLicenseApplications`
- GET `/api/LocalDrivingLicenseApplications/{id}`
- GET `/api/LocalDrivingLicenseApplications/{localId}/application-basic-info`
- GET `/api/LocalDrivingLicenseApplications/application/{applicationId}`
- GET `/api/LocalDrivingLicenseApplications/license-class/{licenseClassId}`
- GET `/api/LocalDrivingLicenseApplications/person/{personId}`
- GET `/api/LocalDrivingLicenseApplications/create-info`
- GET `/api/LocalDrivingLicenseApplications/{localId}/application-id`
- POST `/api/LocalDrivingLicenseApplications`
- PUT `/api/LocalDrivingLicenseApplications/{id}`
- DELETE `/api/LocalDrivingLicenseApplications/{id}`
- POST `/api/LocalDrivingLicenseApplications/{id}/cancel`

The list endpoint supports PaginationRequest.

## Drivers — StaffOnly

- GET `/api/Drivers`
- GET `/api/Drivers/{id}`
- GET `/api/Drivers/person/{personId}`
- GET `/api/Drivers/created-by/{userId}`
- POST `/api/Drivers`
- PUT `/api/Drivers/{id}`
- DELETE `/api/Drivers/{id}`

## Dashboard

- GET `/api/Dashboard/statistics`

## Countries

- GET `/api/Countries`

## License Classes

- GET `/api/LicenseClasses`
- GET `/api/LicenseClasses/{id}`

## Application Types — AdminOnly

- GET `/api/ApplicationTypes`
- GET `/api/ApplicationTypes/{id}`
- PUT `/api/ApplicationTypes/{id}`

## Test Types

- GET `/api/TestTypes`
- GET `/api/TestTypes/{id}`
- PUT `/api/TestTypes/{id}` — AdminOnly

## Test Workflow — StaffOnly

- GET `/api/TestWorkflow/can-schedule?localAppId={id}&testType={type}`
- GET `/api/TestWorkflow/next-test/{localAppId}`
- GET `/api/TestWorkflow/can-take/{appointmentId}`

## Other controller areas

The repository also contains TestAppointments, Tests, Licenses, LicenseIssuance, LicenseRenewal, LicenseReplacement, DetainedLicenses, InternationalLicenses, and Users controllers.

**Needs verification:** use the current controller source as the final authority for every action in these controllers before publishing a client-facing OpenAPI contract. The dedicated controller files are the source of truth.

## Request/response contracts

Confirmed examples:

- LoginRequest / LoginResponse
- IssueFirstLicenseRequest / IssueFirstLicenseResponse
- RenewLicenseRequest
- ReplaceLicenseRequest
- CreateDetainedLicenseRequest
- ReleaseDetainedLicenseRequest
- ScheduleTestRequest
- SaveTestResultRequest

## Typical HTTP outcomes

- 200 OK
- 201 Created
- 204 No Content
- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 409 Conflict
- 500-level unexpected failure
