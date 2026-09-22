# DVLD Security Documentation

## Authentication

JWT bearer authentication is implemented.

`POST /api/Auth/login` is anonymous while the controller is authenticated by default.

## Authorization

Policy-based authorization is used, including:

- AdminOnly
- StaffOnly
- StaffOrAdmin

## Password security

Passwords are hashed using BCrypt before persistence. Plaintext passwords must never be stored.

## Rate limiting

The login endpoint uses the `LoginRateLimit` policy.

## Current user

The application uses a current-user abstraction to access authenticated identity without spreading HTTP context through business services.

## Database security

Integrity is reinforced through foreign keys, restrictive delete behavior, unique indexes, filtered unique indexes, required fields, and Application-level validation.

## Concurrency

Concurrency-sensitive workflows use transaction protection. Test-result recording uses Serializable isolation and has integration tests for simultaneous submissions.

## Secrets

Never commit JWT keys, database passwords, production connection strings, API secrets, or deployment credentials.

## Future improvements

Potential improvements, not current implementation claims:

- Centralized ProblemDetails
- Security headers
- Refresh-token rotation if required
- Secret-vault integration
- Audit logging
- Correlation IDs
- Account lockout
- Dependency vulnerability scanning
- Automated API security tests
