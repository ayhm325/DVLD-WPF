# DVLD Security Documentation

## 1. Security model

The API (`DVLD.Api`) is the authoritative security boundary.

Client applications (`Presentation (WPF)` and `ClientApp`) may hide or disable functionality based on role or workflow state, but client-side UI restrictions are not authorization controls.

## 2. Authentication

JWT bearer authentication is implemented.

`POST /api/Auth/login` is anonymous and rate limited. Other Auth operations require authentication.

A successful login returns:

- access token
- expiration time
- user ID
- username
- person ID
- full name
- role

The WPF client stores the access token in the in-memory current-user session (`CurrentUserService`). Remember Me persists username/preferences, not an authentication token. The React client (`ClientApp`) similarly holds the access token in in-memory component state (see [`ClientApp/docs/API-INTEGRATION.md`](../ClientApp/docs/API-INTEGRATION.md)).

## 3. JWT validation

JWT validation checks:

- issuer
- audience
- signing key
- lifetime

Clock skew is zero.

Startup validation requires:

- non-empty secret
- secret of at least 32 bytes
- issuer
- audience
- positive expiration period

## 4. Active-user validation

During token validation (`JwtBearerEvents.OnTokenValidated` in `API/DVLD.Api/Program.cs`), the API extracts the authenticated user identifier (`ClaimTypes.NameIdentifier`) and verifies via `IUserService.IsUserActiveAsync` that the user exists and is active in the database.

An inactive or missing user causes token validation to fail with HTTP 401.

## 5. Authorization policies

| Policy | Required Role(s) in `Program.cs` |
|---|---|
| `AdminOnly` | `Admin` (`RequireRole("Admin")`) |
| `StaffOnly` | `Staff` (`RequireRole("Staff")`) |
| `StaffOrAdmin` | `Staff` or `Admin` (`RequireRole("Staff", "Admin")`) |
| `Authenticated` (`[Authorize]`) | Any authenticated, active user identity |

Current controller usage includes:

- `AdminOnly`: administrative resources (`UsersController`, `ApplicationTypesController`, `PUT /api/TestTypes/{id}`)
- `StaffOnly`: operational workflows (`Applications`, `DetainedLicenses`, `Drivers`, `InternationalLicenses`, `LicenseIssuance`, `LicenseRenewal`, `LicenseReplacement`, `Licenses`, `LocalDrivingLicenseApplications`, `TestAppointments`, `Tests`, `TestWorkflow`)
- `StaffOrAdmin`: `PeopleController`
- `Authenticated`: shared/read-oriented resources (`Auth` authenticated actions, `Countries`, `Dashboard`, `LicenseClasses`, `GET /api/TestTypes`)

Note that `StaffOnly` strictly requires the `Staff` role and does not include `Admin`; `Admin` and `Staff` permissions are segregated on `StaffOnly` and `AdminOnly` endpoints unless `StaffOrAdmin` or general `[Authorize]` is used.

## 6. CORS policy

`API/DVLD.Api/Program.cs` registers and applies a named CORS policy `"ReactApp"`:

- Allowed origin: `http://localhost:5173` (`WithOrigins("http://localhost:5173")`)
- Allowed headers: any (`AllowAnyHeader()`)
- Allowed methods: any (`AllowAnyMethod()`)

`app.UseCors("ReactApp")` is placed between `app.UseRouting()` and `app.UseRateLimiter()` / `app.UseAuthentication()`.

## 7. Login rate limiting

The login endpoint uses `LoginRateLimit`:

- fixed window
- 5 requests per IP
- 1-minute window
- QueueLimit = 0

Requests exceeding the limit receive HTTP 429 according to ASP.NET Core rate-limiting behavior.

A dedicated automated 429 threshold test is not claimed unless explicitly present in the test suite.

## 8. Password security

Passwords are hashed using BCrypt (`BCrypt.Net-Next`) in the Application layer (`UserService` and `AuthService`) before persistence.

Plaintext passwords are not intended to be stored.

## 9. Current-user abstraction

Application services depend on `ICurrentUserService` rather than directly depending on ASP.NET `HttpContext`.

The API provides the implementation (`ApiCurrentUserService` in `API/DVLD.Api/Security/ApiCurrentUserService.cs`). This keeps HTTP-specific identity access outside the core Application business layer.

## 10. Error handling and information disclosure

Unexpected exceptions are handled centrally using:

- `GlobalExceptionHandler` (`API/DVLD.Api/Security/GlobalExceptionHandler.cs`)
- `AddProblemDetails`
- `UseExceptionHandler`

Detailed exceptions are logged server-side.

Clients receive generic ProblemDetails (HTTP 500) for unexpected failures instead of raw exception details, including a `traceId` extension for diagnostic correlation.

When `GlobalExceptionHandler` handles an `OperationCanceledException` while `context.RequestAborted.IsCancellationRequested` is true, this project's `GlobalExceptionHandler` logs the cancellation at `Information` level and sets HTTP status code `499` (`StatusCodes.Status499ClientClosedRequest`) if the response has not yet started, rather than logging an unhandled server error or returning a 500 ProblemDetails payload.

## 11. Result-to-HTTP mapping

| Result | HTTP |
|---|---:|
| Validation | 400 |
| Authentication failure | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Unexpected Failure | 500 |

ProblemDetails and centralized exception handling are implemented capabilities.

## 12. Database integrity

Security and integrity are reinforced through:

- foreign keys
- restrictive delete behavior
- unique constraints
- filtered unique indexes
- required fields
- length constraints
- Application validation
- transactions

## 13. Concurrency protection

Serializable transactions (coordinated through `UnitOfWork` with deferred physical commit and ambiguous-commit verification) are used for critical workflows including:

- test-result recording
- first-license issuance
- renewal
- replacement
- detention
- detention release
- international-license issuance

Database uniqueness constraints provide an additional protection layer.

## 14. Transport security

The API enables HTTPS redirection.

The repository does not establish a production certificate/deployment configuration, so production TLS operations are not claimed here.

## 15. Secrets

Never commit:

- JWT signing keys
- database passwords
- production connection strings
- API secrets
- deployment credentials

Test secrets are supplied through pipeline configuration.

## 16. Security testing

Automated tests cover authentication/authorization behavior, HTTP authorization outcomes, business error mapping, database constraints, transaction behavior, and concurrency-sensitive workflows.

Most controller and workflow integration tests use `TestAuthenticationHandler` for controlled identity injection so policy behavior can be verified deterministically. In addition, `AuthControllerTests` (`Test/API.IntegrationTests/Controllers/AuthControllerTests.cs`) includes dedicated integration tests (`JwtBearer_WhenTokenIsValidAndUserIsActive_AllowsAccessViaUserService` and `JwtBearer_WhenUserIsInactiveOrMissing_ReturnsUnauthorizedViaUserService`) that re-enable `JwtBearerDefaults.AuthenticationScheme`, generate a signed token with `IJwtTokenService`, and verify both JWT validation and the `OnTokenValidated` active-user check (`IUserService.IsUserActiveAsync`).

## 17. Future hardening

Potential future improvements, not current implementation claims:

- security response headers
- refresh-token rotation if required
- production secret-vault integration
- stronger audit logging
- correlation-ID infrastructure beyond trace IDs
- account lockout
- dependency vulnerability scanning
- dedicated API abuse/security tests
