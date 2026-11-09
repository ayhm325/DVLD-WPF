# Security Architecture & Authorization Standards

> **Authority Level:** Mandatory Security Baseline  
> **Parent Document:** [`AGENTS.md`](./AGENTS.md)  
> **Master Plan Reference:** [`docs/PROJECT-COMPLETION-MASTER-PLAN.md`](./docs/PROJECT-COMPLETION-MASTER-PLAN.md)  
> **Repository Root:** `D:\DVLD-WPF`  
> **Baseline Commit:** `29e43d6ce26ff6e7f516c45f4f475b10bdc2bfd3`  

---

## 1. Threat Model & Core Security Principles

The DVLD application is an enterprise government department management system. Authenticated actors are internal staff members and administrators managing citizen licensing records. Citizens do not hold user accounts or directly access the API.

The core security principles are:
1. **Defense in Depth:** The backend API is the absolute security boundary. Frontend route guards and UI role filtering are usability features, not security controls.
2. **Segregation of Duties (SoD):** The current API implementation enforces distinct operational roles between administrative personnel and counter staff, aligned with approved ADR-O01 (Model B).
3. **Fail-Closed Authorization:** Endpoints without explicit anonymous attributes require authentication. Endpoints modifying state require explicit role policies.
4. **Secret Confidentiality:** No passwords, signing keys, connection strings, or hash digests may ever be committed, logged, or documented in plaintext.

---

## 2. Authentication Architecture

Authentication is stateless and implemented using JWT Bearer tokens signed via HMAC-SHA256.

```mermaid
sequenceDiagram
    autonumber
    actor User as Staff / Admin
    participant Client as ClientApp / WPF
    participant Auth as AuthController
    participant JWT as JwtTokenService
    participant API as Protected Endpoints

    User->>Client: Enter Credentials
    Client->>Auth: POST /api/Auth/login (Rate Limited)
    Auth->>Auth: Verify BCrypt Hash (WorkFactor = 11)
    Auth->>JWT: Generate JWT Token (Claims: Sub, Name, Role)
    JWT-->>Client: Return Token & UserDto
    Client->>API: HTTP Request with Bearer Token
    API->>API: Validate Signature & Expiry
    API->>API: JwtBearerEvents.OnTokenValidated (Check IsActive in DB)
    API-->>Client: 200 OK / Authorized Response
```

### 2.1 Dynamic Revocation Gate (`OnTokenValidated`)
In `Program.cs` (lines 148–172), `JwtBearerEvents.OnTokenValidated` intercepts every authenticated HTTP request:
- Extracts `UserId` from `ClaimTypes.NameIdentifier`.
- Resolves `IUserService` from the request-scoped service provider.
- Queries `await userService.IsUserActiveAsync(userId)`.
- If the user has been deactivated in SQL Server (`IsActive = 0`), execution immediately calls `context.Fail("User is inactive.")` and terminates the request.
- **Security Guarantee:** Deactivating a user in the database immediately revokes their API access without requiring token expiration or distributed cache blacklists.

### 2.2 Password Security & Secret Storage
- **Password Hashing:** Implemented in `AuthService.cs` and `UserService.cs` using `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11)`.
- **Secret Key Management:** The JWT signing key is configured in `appsettings.json` under `JwtSettings:SecretKey`. In staging/production environments, this value **must** be injected via environment variables or Azure Key Vault. Falling back to hardcoded development secrets in production is strictly prohibited.

---

## 3. Authorization Architecture & Policy Matrix

The API configures three distinct authorization policies in `Program.cs`:
- `AdminOnly`: `policy.RequireRole("Admin")`
- `StaffOnly`: `policy.RequireRole("Staff")`
- `StaffOrAdmin`: `policy.RequireRole("Admin", "Staff")`

### 3.1 The Canonical Role & Permission Matrix

| Controller / Endpoint Area | Active Authorization Policy | Admin Access | Staff Access | Architectural Rationale |
|---|---|---|---|---|
| **`UsersController`** (`/api/users/*`) | `[Authorize("AdminOnly")]` | **Allowed** | Denied | User management, password resets, role assignment. |
| **`ApplicationsController`** (`/api/applications/*`) | `[Authorize("StaffOnly")]` | Denied | **Allowed** | Operational licensing counter transactions (Segregation of Duties). |
| **`LicenseIssuanceController`** (`/api/licenses/issue-*`) | `[Authorize("StaffOnly")]` | Denied | **Allowed** | Issuing driver licenses upon test completion. |
| **`LicenseRenewalController`** (`/api/licenses/renew`) | `[Authorize("StaffOnly")]` | Denied | **Allowed** | Processing license renewals. |
| **`LicenseReplacementController`** (`/api/licenses/replace`) | `[Authorize("StaffOnly")]` | Denied | **Allowed** | Processing lost/damaged replacements. |
| **`DetainedLicensesController`** (`/api/detained-licenses/*`) | `[Authorize("StaffOnly")]` | Denied | **Allowed** | Detaining and releasing citizen licenses. |
| **`TestAppointmentsController`** (`/api/test-appointments/*`) | `[Authorize("StaffOrAdmin")]` | **Allowed** | **Allowed** | Scheduling and viewing test appointments. |
| **`TestsController`** (`/api/tests/*`) | `[Authorize("StaffOrAdmin")]` | **Allowed** | **Allowed** | Recording test results and exam lockouts. |
| **`PeopleController`** (`/api/people/*`) | `[Authorize("StaffOrAdmin")]` | **Allowed** | **Allowed** | Viewing and updating citizen demographic profiles. |
| **`AuthController.ChangePassword`** | `[Authorize]` | **Allowed** | **Allowed** | Authenticated session self-service password update. |

### 3.2 Approved Decision: Segregation of Duties (ADR-O01 / SEC-01)
- **Current Technical Implementation:** The active API implementation enforces `[Authorize("StaffOnly")]`, which strictly excludes `Admin` users from executing operational licensing counter actions. This behavior is reflected in `ClientApp/permissions.ts` and verified by unit test `ApplicationsControllerTests.cs:L35`.
- **Approved Architectural Policy:** In [`ENGINEERING-DECISIONS.md`](./ENGINEERING-DECISIONS.md), `ADR-O01` is formally recorded as: `APPROVED — Model B (Explicit Permission Separation)`. Administrators do not automatically inherit counter staff operational permissions; staff do not receive administrative user-management permissions; shared capabilities are explicitly granted via `StaffOrAdmin`.
- **Preservation of Current Behavior & Authorization Boundary:** Existing authorization behavior must remain unchanged. Approval of ADR-O01 records architectural policy agreement, but does **not** authorize code refactoring or policy relaxation without an authorized implementation milestone. Engineers and AI agents are strictly prohibited from changing `StaffOnly` to `StaffOrAdmin` or altering role access rules without explicit written Project Owner authorization.

---

## 4. Input Validation & File Upload Security

### 4.1 Citizen Profile Photos (`PeopleController` & `PhysicalFileStorageService`)
Profile photo uploads undergo strict multi-stage validation and sanitization before reaching physical storage:
1. **Allowed Formats & Re-Encoding:** Accepts `image/jpeg`, `image/png`, and `image/webp`. All accepted images are sanitized, oriented, resized to portrait proportions ($\le 600 \times 800$ px), stripped of EXIF metadata, and re-encoded to WebP format at quality 85 via SkiaSharp (`ImageProcessingService.cs`).
2. **File Size Limits:** Upload stream size is strictly capped at 5 MB (`5,242,880 bytes`).
3. **Decompression Bomb Protection:** Pre-decode validation enforces maximum dimensions of 4096 px and a maximum total pixel count of 16,000,000 pixels.
4. **Magic Byte Verification:** File headers are validated against constant byte arrays before decoding:
   - JPEG: `0xFF, 0xD8, 0xFF`
   - PNG: `0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A`
   - WebP: `RIFF` and `WEBP` signatures
5. **Path Traversal Prevention & Server-Generated Keys:** Storage keys are generated as random GUIDs (`Guid.NewGuid():N + ".webp"`). Original client-supplied filenames, extensions, and directory paths are discarded.
6. **Storage Location:** Files reside strictly outside `wwwroot` in an isolated directory configured by `Storage:PersonImagesPath` (defaulting to `LocalAppData/DVLD/Storage/people`). Direct public HTTP execution or browsing is impossible.
7. **Authorization & Access:** Photo upload, retrieval, and deletion endpoints on `PeopleController` enforce `[Authorize("StaffOrAdmin")]`. Public unauthenticated access is forbidden. Image streaming occurs via authenticated HTTP GET with ETag and private caching headers (`Cache-Control: private, max-age=86400, must-revalidate`).
8. **File/Database Consistency & Compensation Lifecycle:**
   - *Upload Save Ordering:* 1) Process and re-encode in memory; 2) Write new WebP file to disk; 3) Update `People.ImagePath` in EF Core DbContext; 4) Commit via `_unitOfWork.SaveChangesAsync()`.
   - *Failure Compensation:* If `SaveChangesAsync()` fails or returns $\le 0$, the newly written file is immediately deleted via `_fileStorageService.DeleteAsync(newKey)` to prevent orphaned disk files.
   - *Old Image Cleanup:* Upon successful database commit of a replacement photo, the previous physical file is deleted asynchronously.
   - *Photo Deletion:* `DELETE /api/people/{id}/photo` sets `ImagePath = null` in the database, commits via UnitOfWork, and then deletes the physical file.
   - *Orphan File Cleanup:* Immediate compensating delete is verified and implemented in code. A periodic background orphan scavenger remains a proposed future improvement.
9. **Absolute Prohibition on Manual DB Restoration:**
   AI agents and automated tools are strictly prohibited from executing manual database restoration scripts or direct filesystem batch updates under `AGENTS.md` Section 5. Disaster recovery and backups are sysadmin infrastructure operations outside agent authority.

### 4.2 Open Business Decision: Detained License Replacement (INT-03)
- **Observed System Behavior:** Static inspection confirms that `LicenseReplacementService` currently does not inject `IDetainedLicenseRepository` and does not check `IsLicenseDetainedAsync` before issuing a replacement.
- **Potential Business-Rule Gap:** Without a detention check, an applicant holding a detained license could technically be issued a replacement license.
- **Unverified Regulatory / Legal Requirements:** Whether replacing a detained license is prohibited by departmental regulations or traffic law is not documented in existing requirements and remains unverified. No legal rule can be assumed or invented.
- **Human Decision Prerequisite:** Formally resolving this gap requires explicit stakeholder/business policy direction (`ADR-O02` in [`ENGINEERING-DECISIONS.md`](./ENGINEERING-DECISIONS.md)). Engineers must not modify `LicenseReplacementService` to block detained license replacements until authorized by an approved business decision.

---

## 5. Rate Limiting & Denial of Service Protection

- **Active Rate Limiting:** `AuthController.Login` is protected by `[EnableRateLimiting("LoginRateLimit")]` using ASP.NET Core's built-in sliding window rate limiter.
- **Required Hardening (SEC-03):** `AuthController.ChangePassword` currently lacks rate limiting. Applying a sliding window rate limiter to prevent automated brute-force attempts on active sessions is scheduled as P3 in [`ENGINEERING-ROADMAP.md`](./ENGINEERING-ROADMAP.md).

---

## 6. Audit Logging & Security Event Tracing

- High-impact events (failed logins, password changes, license revocations, administrative user deactivations) must be logged at `LogLevel.Warning` or `LogLevel.Information` with contextual metadata (User ID, IP address, timestamp).
- Sensitive values (plain text passwords, JWT signing keys, credit card or banking details) must **never** be logged. Structured logging templates must sanitize user inputs.

---

## 7. Canonical Risk Register & Historical Inventory Mapping

The DVLD platform security governance maintains complete traceability between the foundational empirical audit findings and the active governance risk inventory:

### 7.1 Historical Baseline vs. Active Governance Inventories
1. **Foundational Phase A Findings (`RSK-S01` through `RSK-S13`):**
   Documented in [`docs/audits/phase-a-security-architecture-audit.md`](./docs/audits/phase-a-security-architecture-audit.md) §6 and preserved in [`docs/archive/security-sources/SECURITY-IMPLEMENTATION-READINESS.md`](./docs/archive/security-sources/SECURITY-IMPLEMENTATION-READINESS.md) §5.1. Represents the 13 empirical security defects and missing controls discovered in the legacy baseline.
2. **Unified Risk Inventory (`RSK-01` through `RSK-20`):**
   Codified in [`docs/archive/security-sources/SECURITY-MASTER-PLAN.md`](./docs/archive/security-sources/SECURITY-MASTER-PLAN.md) §3 and adopted across all active governance documents ([`ENGINEERING-STANDARDS.md`](./ENGINEERING-STANDARDS.md), [`ENGINEERING-DECISIONS.md`](./ENGINEERING-DECISIONS.md), [`TESTING-STANDARDS.md`](./TESTING-STANDARDS.md), and [`ENGINEERING-ROADMAP.md`](./ENGINEERING-ROADMAP.md)). Incorporates the 13 remapped original findings plus 7 additional architectural, threat scenario, and operational records ($13 + 7 = 20$).

### 7.2 Authoritative 13-to-20 Mapping Ledger

| Original Phase A ID | Unified Active Governance ID | Canonical Risk Title | Active Governance Source | Attached Security Package / Gate |
|:---:|:---:|---|---|---|
| **`RSK-S01`** | **`RSK-04`** | Stateless 4-Hour Access Token Without Revocation Capability | `ENGINEERING-STANDARDS.md` §9.1 | `SEC-PKG-10` / `DEC-AUTH-01` |
| **`RSK-S02`** | **`RSK-01`** | Orphaned Application Completion Route Workflow Bypass | `ENGINEERING-ROADMAP.md` (`INT-01`) | `SEC-PKG-01` / `ADR-P03`, `DEC-APP-01` |
| **`RSK-S03`** | **`RSK-05`** | Missing Account-Level Lockout Defense & Denial-of-Service Risk | `ENGINEERING-STANDARDS.md` §9.4 | `SEC-PKG-11` / `DEC-CRED-02`, Schema Gate 5 |
| **`RSK-S04`** | **`RSK-06`** | Absence of Dedicated Security Audit Trail & Event Framework | `ENGINEERING-STANDARDS.md` §10.1 | `SEC-PKG-06` / `SEC-04`, `DEC-AUD-01/02` |
| **`RSK-S05`** | **`RSK-13`** | Circumvention of License Detention in Replacement Workflow | `ENGINEERING-STANDARDS.md` §9.10 | `SEC-PKG-15` / `INT-03`, `ADR-O02`, Gate 3 |
| **`RSK-S06`** | **`RSK-14`** | Circumvention of Detention in International Permit Issuance | `ENGINEERING-STANDARDS.md` §9.10 | `SEC-PKG-16` / `DEC-POL-02`, Gate 3 |
| **`RSK-S07`** | **`RSK-02`** | ImagePath Cross-User Deletion within Storage Root | `ENGINEERING-STANDARDS.md` §9.6 | `SEC-PKG-02` / `DEC-FILE-01` |
| **`RSK-S08`** | **`RSK-03`** | Credential Verification Latency Timing Side-Channel | `ENGINEERING-STANDARDS.md` §9.3 | `SEC-PKG-08` (Constant-Time Dummy Hash) |
| **`RSK-S09`** | **`RSK-09`** | Hardcoded Administrator Protections & Single Points of Failure | `ENGINEERING-STANDARDS.md` §9.5 | `SEC-PKG-04` (Dynamic Admin Safety) |
| **`RSK-S10`** | **`RSK-07`** | Weak Password Complexity Policy & Absence of Breached Checks | `ENGINEERING-STANDARDS.md` §9.3 | `SEC-PKG-09` / `DEC-CRED-01` (NIST SP 800-63B) |
| **`RSK-S11`** | **`RSK-10`** | Unbounded Collection Retrieval via Dual-Behavior Endpoints | `ENGINEERING-STANDARDS.md` §9.7 | `SEC-PKG-03` (Pagination Clamping $\le 50$) |
| **`RSK-S12`** | **`RSK-16`** | Missing HTTP Security Headers & Unconfigured Trusted Proxies | `ENGINEERING-STANDARDS.md` §9.8 | `SEC-PKG-12` / `DEC-DEP-01`, `DEC-DEP-02` |
| **`RSK-S13`** | **`RSK-12`** | ADR-O01 Model B License History Authorization Mismatch | `ENGINEERING-STANDARDS.md` §9.2 | `SEC-PKG-05` / `DEC-RBAC-01` |

### 7.3 The Seven Additional Risk Records

| Active Governance ID | Canonical Title | Active Governance Source | Scope & Classification | Governance Status |
|:---:|---|---|---|---|
| **`RSK-08`** | Absence of Multi-Factor Authentication (MFA) for Administrative Roles | `ENGINEERING-DECISIONS.md` (`DEC-AUTH-03`) | Identity & Access Governance | **DEFERRED TO FUTURE PHASE** (`DEC-AUTH-03`) |
| **`RSK-11`** | Plaintext PII Exposure in Citizen & User DTOs | `ENGINEERING-DECISIONS.md` §10.1, `ENGINEERING-ROADMAP.md` §6 | Data Minimization / Privacy | **DEFERRED RISK / PRODUCTION GATE 8 SIGN-OFF REQUIRED** |
| **`RSK-15`** | ADR-O01 Model B Administrative Read Visibility Restriction | `TESTING-STANDARDS.md` §7.5 | Civic Policy Tension | Governed under `DEC-RBAC-01` (Model B Policy Preserved) |
| **`RSK-17`** | Production Configuration & Plaintext Secret Exposure Hazards | `ENGINEERING-STANDARDS.md` §9.8 | Deployment & Infrastructure | Governed under `SEC-PKG-13` / `DEC-DEP-01` |
| **`RSK-18`** | Absence of Automated Dependency Scanning in CI/CD Pipeline | `ENGINEERING-STANDARDS.md` §9.9 | Build & Pipeline Security | Governed under `SEC-PKG-14` (Separate from `OBS-02`) |
| **`RSK-19`** | Unrepresented External Callers for CompleteApplication Endpoint | `ENGINEERING-STANDARDS.md` §9.11 | API Contract Deprecation | Governed under `DEC-APP-01` Telemetry Window (Gate 6) |
| **`RSK-20`** | Authorized Insider Mass PII Harvesting via Unbounded Collections | `TESTING-STANDARDS.md` §7.13 | Threat Scenario | Mitigated under `SEC-PKG-03` (Clamping $\le 50$) |

> [!IMPORTANT]
> **Definitive Clarification on `RSK-08` & `RSK-11` (Gate 8):**
> - Foundational empirical finding **`RSK-S08`** (Timing Side-Channel) remapped to **`RSK-03`** (`SEC-PKG-08` / `TST-AUTH-03`).
> - Identifier **`RSK-08`** is exclusively assigned to **Multi-Factor Authentication (MFA) for Administrative Roles** (`DEC-AUTH-03`).
> - Identifier **`RSK-11`** governs citizen PII and photograph/biometric exposure. Production Approval Gate 8 requires formal legal and regulatory review regarding data privacy and statutory compliance under the Jordan Personal Data Protection Law (Law No. 24 of 2023). Engineering data minimization and masking practices are distinguished from confirmed statutory obligations, and legal verification remains an outstanding project item.
> - There is zero duplication across active or historical risk registers.

---

## 12. AUTH-01 Session Management Standard

1. **Access tokens:** 15-minute HS256 JWT with `sub`, `name`, `role`, `FullName`, `PersonId`, `sst` (security stamp), and `jti`. No secrets or sensitive personal data in claims.
2. **Refresh tokens:** 256-bit CSPRNG values delivered only in an `HttpOnly`, `SameSite=Lax`, `Secure`-when-HTTPS cookie scoped to `/api/auth`. Only the Base64Url SHA-256 hash is stored (`UserSessions.RefreshTokenHash`, unique index).
3. **Session model:** one row per issued refresh generation; `TokenFamilyId` groups a login and `ReplacedBySessionId` records rotation. Absolute lifetime 14 days (never extended); idle lifetime 7 days.
4. **Rotation:** every refresh consumes the current generation through one conditional `UPDATE ... WHERE RevokedAtUtc IS NULL AND ExpiresAtUtc > now`. Exactly one concurrent caller may win; losers receive 409 and retry once.
5. **Reuse detection:** a consumed token presented after a 30-second grace window revokes the whole family and returns a generic 401. Inside the grace window with a still-active family it returns 409 without revocation (multi-tab tolerance).
6. **Immediate invalidation:** `User.SecurityStamp` rotates on password change, role change, and activation change. `OnTokenValidated` compares the `sst` claim with the persisted stamp on every request.
7. **Revocation triggers:** logout (current session), logout-all (all user sessions), password change (all + fresh session for the current device), role/activation change (all + stamp rotation), deletion (all).
8. **CSRF:** `SameSite=Lax` cookie + explicit credential-enabled CORS origin + mandatory `X-CSRF-Protection: 1` header on refresh/logout. Refresh and logout are anonymous because the cookie is the credential; the access token is not required.
9. **Client storage:** access tokens are held in memory only. Refresh tokens are never readable by JavaScript and never stored in `localStorage`/`sessionStorage`. Legacy token keys are purged at startup.
10. **Logging:** raw refresh tokens, JWTs, passwords, and signing keys must never be logged. Security events log user id and token-family id only.
