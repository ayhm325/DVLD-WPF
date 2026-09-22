# DVLD API Reference

## 1. Overview

The API is an ASP.NET Core Web API exposed under `/api/[controller]`.

The current source contains **20 controllers and 101 HTTP actions**.

The source-based count is authoritative for this documentation. An earlier count of 100 was caused by counting the Users controller as seven actions instead of its current eight.

## 2. Base URL

The local HTTP launch profile has been used at:

```text
http://localhost:5260
```

Current launch settings are authoritative if the port changes.

## 3. Authentication

JWT bearer authentication protects the API except:

```http
POST /api/Auth/login
```

A successful login returns:

- AccessToken
- ExpiresAtUtc
- UserId
- UserName
- PersonId
- FullName
- Role

Clients send:

```http
Authorization: Bearer <access-token>
```

## 4. Authorization policies

| Policy | Meaning |
|---|---|
| Authenticated | Any authenticated user |
| StaffOnly | Staff role |
| StaffOrAdmin | Staff or Admin |
| AdminOnly | Admin role |

## 5. HTTP response conventions

Common successful responses:

- `200 OK`
- `201 Created`
- `204 No Content`

Expected Result mapping:

| Result | HTTP |
|---|---:|
| Validation | 400 |
| Authentication failure | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Unexpected failure | 500 |

Unexpected exceptions are handled centrally by `GlobalExceptionHandler` and returned as generic ProblemDetails.

## 6. Complete endpoint inventory

### Auth — 4

| Method | Route | Access |
|---|---|---|
| POST | `/api/Auth/login` | Anonymous + rate limited |
| GET | `/api/Auth/me` | Authenticated |
| GET | `/api/Auth/profile` | Authenticated |
| POST | `/api/Auth/change-password` | Authenticated |

### Applications — 8 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/Applications` |
| GET | `/api/Applications/{id}` |
| GET | `/api/Applications/{id}/basic-info` |
| POST | `/api/Applications` |
| PUT | `/api/Applications/{id}` |
| DELETE | `/api/Applications/{id}` |
| POST | `/api/Applications/{id}/complete` |
| POST | `/api/Applications/{id}/cancel` |

### ApplicationTypes — 3 — AdminOnly

| Method | Route |
|---|---|
| GET | `/api/ApplicationTypes` |
| GET | `/api/ApplicationTypes/{id}` |
| PUT | `/api/ApplicationTypes/{id}` |

### Countries — 1 — Authenticated

| Method | Route |
|---|---|
| GET | `/api/Countries` |

### Dashboard — 1 — Authenticated

| Method | Route |
|---|---|
| GET | `/api/Dashboard/statistics` |

### DetainedLicenses — 6 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/DetainedLicenses` |
| GET | `/api/DetainedLicenses/{id}` |
| GET | `/api/DetainedLicenses/license/{licenseId}/active` |
| GET | `/api/DetainedLicenses/license/{licenseId}/detained` |
| POST | `/api/DetainedLicenses` |
| POST | `/api/DetainedLicenses/release` |

### Drivers — 7 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/Drivers` |
| GET | `/api/Drivers/{id}` |
| GET | `/api/Drivers/person/{personId}` |
| GET | `/api/Drivers/created-by/{userId}` |
| POST | `/api/Drivers` |
| PUT | `/api/Drivers/{id}` |
| DELETE | `/api/Drivers/{id}` |

### InternationalLicenses — 7 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/InternationalLicenses` |
| GET | `/api/InternationalLicenses/{internationalLicenseId}` |
| GET | `/api/InternationalLicenses/driver/{driverId}` |
| GET | `/api/InternationalLicenses/application/{applicationId}` |
| GET | `/api/InternationalLicenses/license/{localLicenseId}` |
| GET | `/api/InternationalLicenses/license/{licenseId}/info` |
| POST | `/api/InternationalLicenses` |

### LicenseClasses — 2 — Authenticated

| Method | Route |
|---|---|
| GET | `/api/LicenseClasses` |
| GET | `/api/LicenseClasses/{id}` |

### LicenseIssuance — 1 — StaffOnly

| Method | Route |
|---|---|
| POST | `/api/LicenseIssuance/first-license` |

### LicenseRenewal — 1 — StaffOnly

| Method | Route |
|---|---|
| POST | `/api/LicenseRenewal` |

### LicenseReplacement — 1 — StaffOnly

| Method | Route |
|---|---|
| POST | `/api/LicenseReplacement` |

### Licenses — 8 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/Licenses` |
| GET | `/api/Licenses/{id}` |
| GET | `/api/Licenses/driver/{driverId}` |
| GET | `/api/Licenses/application/{applicationId}` |
| GET | `/api/Licenses/license-class/{licenseClassId}` |
| GET | `/api/Licenses/person/{personId}` |
| GET | `/api/Licenses/local-application/{localAppId}/details` |
| GET | `/api/Licenses/{licenseId}/details` |

### LocalDrivingLicenseApplications — 12 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/LocalDrivingLicenseApplications` |
| GET | `/api/LocalDrivingLicenseApplications/{id}` |
| GET | `/api/LocalDrivingLicenseApplications/{localId}/application-basic-info` |
| GET | `/api/LocalDrivingLicenseApplications/application/{applicationId}` |
| GET | `/api/LocalDrivingLicenseApplications/license-class/{licenseClassId}` |
| GET | `/api/LocalDrivingLicenseApplications/person/{personId}` |
| GET | `/api/LocalDrivingLicenseApplications/create-info` |
| GET | `/api/LocalDrivingLicenseApplications/{localId}/application-id` |
| POST | `/api/LocalDrivingLicenseApplications` |
| PUT | `/api/LocalDrivingLicenseApplications/{id}` |
| DELETE | `/api/LocalDrivingLicenseApplications/{id}` |
| POST | `/api/LocalDrivingLicenseApplications/{id}/cancel` |

The main list endpoint uses the application's pagination request model.

### People — 6 — StaffOrAdmin

| Method | Route |
|---|---|
| GET | `/api/People` |
| GET | `/api/People/{id}` |
| GET | `/api/People/national/{nationalNo}` |
| POST | `/api/People` |
| PUT | `/api/People/{id}` |
| DELETE | `/api/People/{id}` |

### TestAppointments — 14 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/TestAppointments` |
| GET | `/api/TestAppointments/{id}` |
| GET | `/api/TestAppointments/local-application/{localAppId}` |
| GET | `/api/TestAppointments/test-type/{testType}` |
| GET | `/api/TestAppointments/schedule-preparation/{localAppId}/{testTypeId}` |
| POST | `/api/TestAppointments/schedule` |
| GET | `/api/TestAppointments/created-by/{userId}` |
| GET | `/api/TestAppointments/{appointmentId}/schedule-info` |
| GET | `/api/TestAppointments/fees/{testTypeId}` |
| GET | `/api/TestAppointments/trial-count` |
| GET | `/api/TestAppointments/scheduled` |
| POST | `/api/TestAppointments` |
| PUT | `/api/TestAppointments/{id}` |
| DELETE | `/api/TestAppointments/{id}` |

### TestTypes — 3

| Method | Route | Access |
|---|---|---|
| GET | `/api/TestTypes` | Authenticated |
| GET | `/api/TestTypes/{id}` | Authenticated |
| PUT | `/api/TestTypes/{id}` | AdminOnly |

### TestWorkflow — 3 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/TestWorkflow/can-schedule?localAppId={id}&testType={type}` |
| GET | `/api/TestWorkflow/next-test/{localAppId}` |
| GET | `/api/TestWorkflow/can-take/{appointmentId}` |

### Tests — 5 — StaffOnly

| Method | Route |
|---|---|
| GET | `/api/Tests` |
| GET | `/api/Tests/{id}` |
| GET | `/api/Tests/appointment/{appointmentId}` |
| GET | `/api/Tests/created-by/{userId}` |
| POST | `/api/Tests` |

### Users — 8 — AdminOnly

| Method | Route |
|---|---|
| GET | `/api/Users` |
| GET | `/api/Users/{id}` |
| GET | `/api/Users/person/{personId}` |
| GET | `/api/Users/{id}/details` |
| GET | `/api/Users/username/{username}` |
| POST | `/api/Users` |
| PUT | `/api/Users/{id}` |
| DELETE | `/api/Users/{id}` |

## 7. Endpoint count

| Controller | Actions |
|---|---:|
| Auth | 4 |
| Applications | 8 |
| ApplicationTypes | 3 |
| Countries | 1 |
| Dashboard | 1 |
| DetainedLicenses | 6 |
| Drivers | 7 |
| InternationalLicenses | 7 |
| LicenseClasses | 2 |
| LicenseIssuance | 1 |
| LicenseRenewal | 1 |
| LicenseReplacement | 1 |
| Licenses | 8 |
| LocalDrivingLicenseApplications | 12 |
| People | 6 |
| TestAppointments | 14 |
| TestTypes | 3 |
| TestWorkflow | 3 |
| Tests | 5 |
| Users | 8 |
| **Total** | **101** |

## 8. ProblemDetails

The API uses `application/problem+json` for centralized error responses.

`GlobalExceptionHandler` logs detailed exceptions server-side and returns generic 500 information with a trace identifier.

## 9. Contracts

`DVLD.Contracts` contains transport-facing request/response models. Application DTOs remain internal to Application and are mapped at the API boundary.

## 10. API design rules

- Controllers remain thin.
- Business workflows belong in Application services.
- EF Core/SQL belongs in Infrastructure.
- Result values are mapped consistently to HTTP.
- Authorization is enforced server-side.
- Multi-write operations define explicit transaction boundaries.
- Transport contracts remain separate from internal Application DTOs.
