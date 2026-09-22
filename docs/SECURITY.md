# DVLD Security Documentation

## 1. Security model

The API is the authoritative security boundary.

The WPF client may hide or disable functionality based on role or workflow state, but client-side UI restrictions are not authorization controls.

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

The WPF client stores the access token in the in-memory current-user session. Remember Me persists username/preferences, not an authentication token.

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

During token validation, the API extracts the authenticated user identifier and verifies that the user exists and is active in the database.

An inactive or missing user causes token validation to fail.

## 5. Authorization policies

| Policy | Meaning |
|---|---|
| AdminOnly | Admin role |
| StaffOnly | Staff role |
| StaffOrAdmin | Staff or Admin |
| Authenticated | Any authenticated identity |

Current controller usage includes:

- AdminOnly: administrative resources
- StaffOnly: operational workflows
- StaffOrAdmin: People
- Authenticated: shared/read-oriented resources

## 6. Login rate limiting

The login endpoint uses `LoginRateLimit`:

- fixed window
- 5 requests per IP
- 1-minute window
- QueueLimit = 0

Requests exceeding the limit receive HTTP 429 according to ASP.NET Core rate-limiting behavior.

A dedicated automated 429 threshold test is not claimed unless explicitly present in the test suite.

## 7. Password security

Passwords are hashed using BCrypt before persistence.

Plaintext passwords are not intended to be stored.

## 8. Current-user abstraction

Application services depend on `ICurrentUserService` rather than directly depending on ASP.NET `HttpContext`.

The API provides the implementation. This keeps HTTP-specific identity access outside the core Application business layer.

## 9. Error handling and information disclosure

Unexpected exceptions are handled centrally using:

- `GlobalExceptionHandler`
- `AddProblemDetails`
- `UseExceptionHandler`

Detailed exceptions are logged server-side.

Clients receive generic ProblemDetails for unexpected failures instead of raw exception details.

ProblemDetails responses include a trace identifier for diagnostic correlation.

## 10. Result-to-HTTP mapping

| Result | HTTP |
|---|---:|
| Validation | 400 |
| Authentication failure | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Unexpected Failure | 500 |

ProblemDetails and centralized exception handling are implemented capabilities.

## 11. Database integrity

Security and integrity are reinforced through:

- foreign keys
- restrictive delete behavior
- unique constraints
- filtered unique indexes
- required fields
- length constraints
- Application validation
- transactions

## 12. Concurrency protection

Serializable transactions are used for critical workflows including:

- test-result recording
- first-license issuance
- renewal
- replacement
- detention
- detention release
- international-license issuance

Database uniqueness constraints provide an additional protection layer.

## 13. Transport security

The API enables HTTPS redirection.

The repository does not establish a production certificate/deployment configuration, so production TLS operations are not claimed here.

## 14. Secrets

Never commit:

- JWT signing keys
- database passwords
- production connection strings
- API secrets
- deployment credentials

Test secrets are supplied through pipeline configuration.

## 15. Security testing

Automated tests cover authentication/authorization behavior, HTTP authorization outcomes, business error mapping, database constraints, transaction behavior, and concurrency-sensitive workflows.

Integration tests use `TestAuthenticationHandler` for controlled identity injection. Therefore they verify authorization policy behavior without claiming to be complete end-to-end tests of production JWT cryptographic validation.

## 16. Future hardening

Potential future improvements, not current implementation claims:

- security response headers
- refresh-token rotation if required
- production secret-vault integration
- stronger audit logging
- correlation-ID infrastructure beyond trace IDs
- account lockout
- dependency vulnerability scanning
- dedicated API abuse/security tests
