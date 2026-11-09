# AUTH-01 — Professional Authentication & Session Management

> **Task:** AUTH-01
> **Date:** 2026-11-09
> **Baseline HEAD at start:** `ba22f65730de0618795e0d021db71bc26a27b6b0`
> **WPF:** untouched. **`DVLDf`:** never accessed. **Schema applied to any database:** no (migration generated only).

---

## 1. Phase A — Existing architecture and findings

### 1.1 Verified current state

| Area | Finding | Severity |
|---|---|---|
| Access token lifetime | 240 minutes (`Jwt:ExpirationMinutes`) | **High** |
| Signing/validation | HS256 symmetric key; issuer/audience/lifetime/signing key validated; `ClockSkew = Zero` | OK |
| Claims | `nameid`, `name`, `FullName`, `PersonId`, `role`; role embedded at issue time | **High** |
| Revocation | None. Only `IsActive` is checked per request (one DB query). Password changes do not invalidate tokens. | **Critical** |
| Role changes | Old role remains effective for up to 240 minutes | **High** |
| Refresh tokens | None. Login returns a single long-lived bearer token. | **High** |
| Token storage (SPA) | Access token persisted in `sessionStorage`; `apiClient` also read legacy `localStorage` keys | **High** |
| Session/cookie transport | No cookies, no CSRF protection; CORS allowed one origin without credentials | Informational |
| Logout | Clears client state only; no server revocation | **High** |
| Account deactivation | Blocks new logins and fails the per-request check; existing tokens invalidated only by that check | Medium |
| Concurrent login | Unlimited independent tokens; no session inventory | Medium |
| Error contract | RFC 7807 ProblemDetails (post REST-01) | OK |
| Frontend on expiry | Auto-logout at expiry; no refresh; any 401 immediately ends the session | **High** |
| Password change | Re-hashes password only; existing tokens remain valid | **Critical** |

### 1.2 Deployment topology

- SPA: `http://localhost:5173`; API: `http://localhost:5260` (README + `launchSettings`).
- Same registrable site (`localhost`), **different origin** → cross-origin, same-site.
- No production reverse-proxy/cookie documentation exists; the design therefore uses settings that are safe for same-site deployment and configurable.

---

## 2. Architecture decisions

| Decision | Choice | Rationale |
|---|---|---|
| Refresh transport | **HttpOnly cookie** `dvld_rt`, `SameSite=Lax`, `Secure` when HTTPS, `Path=/api/auth` | Refresh token never readable by JavaScript; works for the verified same-site topology; no token in JSON |
| Access transport | JSON response, held **in memory** in the SPA | Never persisted; XSS cannot read a stored token |
| CSRF | `SameSite=Lax` + explicit CORS origin with credentials + mandatory `X-CSRF-Protection: 1` header on refresh/logout | Custom header forces a preflight that a foreign origin cannot satisfy |
| Access token lifetime | **15 minutes** | Limits stolen-token exposure; silent refresh keeps UX |
| Refresh token | 256-bit random, **SHA-256 hash only** persisted | A database leak does not expose usable tokens |
| Session model | One `UserSessions` row per issued refresh token; `TokenFamilyId` groups a login | Enables rotation chains and family revocation |
| Absolute expiry | 14 days per family, never extended by rotation | Hard cap on session lifetime |
| Idle expiry | 7 days since `LastUsedAtUtc` | Stolen idle tokens stop working |
| Rotation | Every refresh consumes the old row via a single conditional `UPDATE` | Exactly one winner across instances; the database row lock is the arbiter |
| Reuse detection | Consumed token presented again → revoke the whole family, generic 401 | Standard theft response |
| Concurrent refresh | Within a 30 s grace window, a consumed token whose family is still active returns **409** (retry), not family revocation | Protects legitimate multi-tab races without masking real reuse |
| JWT invalidation | `sst` security-stamp claim checked against the DB on every request | Password/role/activation changes take effect immediately |
| Logout | Revoke current session by hash; idempotent | Server-side invalidation |
| Logout all | Revoke all active sessions for the user | Incident response |
| Password change | Revoke all sessions + rotate stamp, then issue a fresh session for the current device | Old credentials/tokens cannot survive |
| Session listing endpoints | **Not implemented** | Logout-all covers the single-organization operational need; avoids new UI/authorization surface |
| Refresh token table vs combined | **Combined `UserSession` generation rows** | One table, explicit chain via `ReplacedBySessionId`; simpler than a separate token table |

---

## 3. Implementation by phase

### Phase B — Session model and token foundation
- `Domain/Entities/UserSession.cs` (new): `Id`, `UserId`, `RefreshTokenHash`, `TokenFamilyId`, `CreatedAtUtc`, `ExpiresAtUtc`, `LastUsedAtUtc`, `RevokedAtUtc`, `RevocationReason`, `ReplacedBySessionId`.
- `User.SecurityStamp` (new, 64 chars, default `NEWID()` for existing rows).
- `Infrastructure/Configurations/UserSessionConfiguration.cs`: unique hash index, family index, `(UserId, RevokedAtUtc)` index, cascade delete.
- `Infrastructure/Repositories/UserSessionRepository.cs`: hash lookup; atomic `TryConsumeAsync`; family/user/hash revocation; active-family probe (all via conditional `ExecuteUpdate`).
- `Application/Services/RefreshTokenService.cs`: 32-byte CSPRNG token, Base64Url SHA-256 hash, constant-time compare, structural validation.
- `AuthSessionOptions` + startup validation; `appsettings.json` `AuthSession` section.

### Phase C — Login, refresh, rotation, reuse, concurrency
- `AuthService.LoginAsync`: validates credentials/active state; creates family; persists hash; returns access token + raw refresh token (cookie-only).
- `AuthService.RefreshAsync`: format check → hash lookup → user active → revocation/expiry/idle checks → atomic consume → new generation → commit.
- Reuse and concurrency policy as described above.

### Phase D — Revocation lifecycle
- `POST /api/auth/logout`: revoke by hash, clear cookie, always 204.
- `POST /api/auth/logout-all`: authenticated, revokes every active session.
- Password change: stamp rotation + revoke all + fresh session for the current device.
- Admin `UserService.UpdateUserAsync`: role or activation change → stamp rotation + revoke all sessions (revocation happens **before** the save, fail-closed).
- `UserService.DeleteUserAsync`: explicit revocation before delete (FK also cascades).
- Per-request JWT validation: `IAuthService.IsPrincipalCurrentAsync` compares `sst` with the persisted stamp.

### Phase E — API and React
- `AuthController`: login/refresh/logout/logout-all/change-password; HttpOnly cookie helpers; CSRF header enforcement; RFC 7807 errors.
- `JwtTokenService`: minimal claims + `sst` + `jti`; 15-minute lifetime.
- `Program.cs`: stamp validation, 30 s clock skew, credential CORS, refresh/change-password rate limits, options validation.
- SPA: in-memory token store, legacy storage purge, single-flight refresh, one retry per request, proactive renewal, cross-tab `BroadcastChannel`, logout API call, startup restoration via the refresh cookie.

---

## 4. Database schema and migration

Migration `20261109163257_AuthSessions` (generated, **not applied to any database**):

- `Users.SecurityStamp nvarchar(64) NOT NULL DEFAULT CONVERT(nvarchar(64), NEWID())`.
- `UserSessions` table with PK (`Id` uniqueidentifier), FK to `Users` (`Cascade`).
- Unique index `IX_UserSessions_RefreshTokenHash`.
- Index `IX_UserSessions_TokenFamilyId`.
- Index `IX_UserSessions_UserId_RevokedAtUtc`.

`Down()` drops the table and the column.

---

## 5. API contracts

| Endpoint | Auth | Contract |
|---|---|---|
| `POST /api/auth/login` | anonymous, rate-limited | 200 `LoginResponse` (access token + user); `Set-Cookie: dvld_rt`; 400 validation; 401 generic |
| `POST /api/auth/refresh` | anonymous cookie + `X-CSRF-Protection: 1` | 200 new access token + rotated cookie; 401 generic + cookie cleared; 409 concurrent retry (cookie preserved) |
| `POST /api/auth/logout` | anonymous cookie + CSRF | 204, cookie cleared, idempotent |
| `POST /api/auth/logout-all` | bearer + CSRF | 204, all sessions revoked |
| `POST /api/auth/change-password` | bearer, rate-limited | 200 fresh `LoginResponse` + new cookie; 400 validation |

Refresh tokens are never present in JSON. Errors remain RFC 7807 `ProblemDetails`.

---

## 6. Security improvements and residual risks

**Improved:** 15-minute access tokens; hash-only refresh persistence; server-side rotation; theft detection with family revocation; immediate invalidation on password/role/activation change; logout/logout-all; HttpOnly cookie transport; CSRF header + same-site cookie + restricted CORS; no tokens in web storage.

**Residual:**
- 30 s reuse grace permits a stolen consumed token to be retried without family revocation inside the window (it still yields no tokens; the legitimate winner remains active). Documented trade-off for multi-tab correctness.
- Security stamp is checked with one DB read per request (existing pattern; performance consideration).
- Cross-process concurrency is **not yet verified**; SQL verification (see addendum) proves single-winner behavior across independent contexts and transactions, not separate OS processes.
- No session-listing UI/endpoint (deliberate scope decision).
- Production deployment must serve API+SPA from the same site (or configure `SameSite=None; Secure`) for the refresh cookie.

> **Addendum (AUTH-01-POST):** SQL persistence, schema, rotation, atomic consume, reuse-after-grace, revocation and rollback were subsequently verified against an isolated LocalDB test database. See [`auth-01-post-sql-verification.md`](./auth-01-post-sql-verification.md).
>
> **Addendum (AUTH-01-CROSS-PROCESS):** Two independent API OS processes sharing one isolated SQL database produced exactly one 200 and one 409 for the same refresh token; reuse-after-grace revoked the family for both processes. See [`auth-01-cross-process-verification.md`](./auth-01-cross-process-verification.md). Lost-response/termination fault injection remains outstanding.

---

## 7. Tests and results

| Suite | Result |
|---|---|
| `Application.UnitTests` (full) | **945 passed, 0 failed, 0 skipped** |
| `API.IntegrationTests` (database-free filter) | **514 passed, 0 failed, 0 skipped** |
| Auth unit focus (`AuthServiceTests` incl. concurrency) | **23 passed** |
| Auth API focus (`AuthControllerTests`, `AuthSessionContractTests`, `JwtTokenServiceClockTests`) | **31 passed** |
| Frontend `npm run lint` | exit 0 |
| Frontend `npm run build` | exit 0 |

Not executed in the AUTH-01 pass: all SQL-backed API workflow suites and `Infrastructure.IntegrationTests`. **Subsequently executed under AUTH-01-POST:** migration application, SQL persistence, atomic rotation, reuse-after-grace, revocation and rollback against an isolated LocalDB database — 12 tests passed. Cross-process concurrency remains unverified. See [`auth-01-post-sql-verification.md`](./auth-01-post-sql-verification.md).

---

## 8. Files changed

**Backend:** `Domain/Entities/User.cs`, `Domain/Entities/UserSession.cs`, `Application/Options/AuthSessionOptions.cs`, `Application/Options/JwtOptions.cs`, `Application/DTOs/AuthDTO/JwtTokenRequest.cs`, `Application/DTOs/AuthDTO/LoginResponseDto.cs`, `Application/Interfaces/{IAuthService,IJwtTokenService,IUserRepository,IUserSessionRepository,IRefreshTokenService}.cs`, `Application/Services/{AuthService,RefreshTokenService,UserService}.cs`, `Infrastructure/{DVLDDbContext.cs,Configurations/UserConfiguration.cs,Configurations/UserSessionConfiguration.cs,Repositories/UserRepository.cs,Repositories/UserSessionRepository.cs}`, `Infrastructure/Migrations/20261109163257_AuthSessions*`, `Infrastructure/Migrations/DVLDDbContextModelSnapshot.cs`, `API/DVLD.Api/{Program.cs,appsettings.json,Controllers/AuthController.cs,Security/JwtTokenService.cs}`.

**Frontend:** `ClientApp/src/utils/authTokenStore.ts`, `utils/sessionStorage.ts`, `services/apiClient.ts`, `services/authService.ts`, `context/AuthContext.tsx`, `context/authContextInstance.ts`, `components/common/PersonPhoto.tsx`, `components/profile/ChangePasswordPage.tsx`.

**Tests:** `Test/Application.UnitTests/Services/{AuthServiceTests,UserServiceTests}.cs`, `Test/API.IntegrationTests/{Controllers/AuthControllerTests.cs,Security/AuthSessionContractTests.cs,Security/JwtTokenServiceClockTests.cs}`.

**Docs:** this report, `ENGINEERING-DECISIONS.md`, `SECURITY-STANDARDS.md`, `TESTING-STANDARDS.md`, `ENGINEERING-ROADMAP.md`, `docs/IMPLEMENTATION-EXECUTION-LOG.md`.
