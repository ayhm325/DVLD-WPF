# AUTH-01-POST — SQL Persistence, Rotation & Cross-Instance Verification

> **Verified commit:** `92028f32cc4bfb8b8485f8444770f039107e084b`
> **Date:** 2026-11-09
> **WPF:** untouched. **`DVLDf`:** never connected to. **Protected databases present on the test instance:** 0.

---

## 1. Result summary

| Area | Result |
|---|---|
| SQL environment | Verified isolated LocalDB instance; **0** protected databases present |
| Migration execution | **Applied** by the ownership-verified fixture to a generated `DVLD_IntegrationTests_<GUID>` database; succeeded |
| Schema verification | **Verified** (table, column, default, indexes, cascade FK) |
| Persistence | **Verified** (hash-only, unique hash, family, chain, revocation, cascade, stamp) |
| Service-level SQL refresh concurrency | **Verified** (exactly one winner; no second chain) |
| Cross-process / two-API-instance | **NOT VERIFIED** (single test process; two independent contexts/UnitOfWork) |
| Reuse after grace | **Verified on SQL** (family revoked, replacement dead) |
| Grace-window 409 path | Covered by existing deterministic unit tests; not re-exercised with a real clock on SQL |
| Failure recovery | **Verified** (failed replacement insert rolls back the consumption) |
| Security stamp / revocation | **Verified at persistence + repository level** |
| Database-free regression | 945 unit + 514 API tests passed |
| Cleanup | **Verified** — zero `DVLD_IntegrationTests_%` databases remained |

**Verdict: IMPLEMENTED — DATABASE-FREE VERIFIED — SQL PERSISTENCE/ROTATION/REUSE/REVOCATION VERIFIED — CROSS-PROCESS CONCURRENCY NOT VERIFIED.**

---

## 2. Baseline

- `git log -1` = `92028f3` (`feat(auth): add persisted refresh token sessions with rotation and revocation`).
- Branch `main`; working tree clean at start.
- No AUTH-01 architecture was reopened. No production source change was required.

## 3. Migration review

`Infrastructure/Migrations/20261109163257_AuthSessions.cs`:

- `Users.SecurityStamp nvarchar(64) NOT NULL DEFAULT CONVERT(nvarchar(64), NEWID())` — existing rows receive unique stamps; verified default exists.
- `UserSessions`: `Id` uniqueidentifier PK, `UserId` FK **Cascade**, `RefreshTokenHash nvarchar(64) NOT NULL`, `TokenFamilyId`, UTC timestamps, nullable revocation fields, `ReplacedBySessionId`.
- Unique index `IX_UserSessions_RefreshTokenHash`; indexes on `TokenFamilyId` and `(UserId, RevokedAtUtc)`.
- `Down()` drops the table and column. No data-destructive operation on existing tables.

The migration matches the EF model; `Database.Migrate()` applied it to the isolated database with no pending-model drift.

## 4. SQL environment and safety

- Instance identity: `<machine-redacted>\LOCALDB#B8C77F08|LOCALDB#B8C77F08`.
- Identity was independently read from `master` before any test database was created.
- `sys.databases` contained **0** databases named `DVLD` or `DVLDf` on this instance.
- The test infrastructure requires `DVLD_TEST_EXPECTED_INSTANCE` and refuses to proceed on mismatch; it rejects `DVLD`/`DVLDf`/unprefixed catalogs, creates `DVLD_IntegrationTests_<GUID>` only, and drops only ownership-verified databases.
- No connection string, secret, or raw token appears in this report.

## 5. Migration execution

Executed only through `SqlServerTestDatabase.InitializeAsync` → `Database.Migrate()` against the generated test database. No `EnsureCreated`, no `database update` CLI, no `DVLDf`.

## 6. Persistence results (9 tests)

`Test/Infrastructure.IntegrationTests/Auth/AuthSessionSqlVerificationTests.cs` — **9 passed**:

1. Schema: table/column/default/unique index/family index/user index/cascade FK/length/nullability.
2. Round-trip: hash persisted, raw token never stored, 43-character Base64Url SHA-256, family preserved.
3. Duplicate hash rejected by the unique index.
4. Atomic consume across two independent contexts: exactly one `true`; stored `ReplacedBySessionId` is the winner's.
5. Rotation preserves absolute expiry, updates idle timestamp, and records the chain.
6. Family revocation revokes all generations and leaves other families active.
7. `RevokeAllForUser` affects only the target user.
8. Logout-by-hash revokes only the matching session.
9. User deletion cascades sessions; security stamp persists and rotates.
10. Failure injection: replacement insert violating the unique index rolls back the consumption (old session remains active; rejected row absent).

## 7. Refresh concurrency (2 tests, real SQL)

`AuthSessionRefreshSqlConcurrencyTests` — **2 passed**:

- Two `AuthService` instances, each with its **own DbContext, repositories, and UnitOfWork**, gated to start together, using the same raw refresh token:
  - exactly **1 success** and **1 conflict**;
  - the family contains exactly **2 rows** (consumed + one replacement);
  - exactly **one active** session;
  - the replacement keeps the original absolute expiry.
- After advancing beyond the 30-second grace window, replaying the consumed token returns 401, revokes the still-active replacement, and the legitimate new token is also rejected thereafter.

**Not proven:** cross-process/third-party-instance concurrency. The guarantee is the single conditional SQL `UPDATE`; this run proves it across independent contexts and transactions in one process, not across separate OS processes.

## 8. Reuse and grace window

- Post-grace replay on SQL: family revoked, generic 401, no token issued.
- Within-grace 409 behavior and its multi-tab trade-off remain covered by deterministic unit tests (injected clock), not by a real-time SQL sleep.
- Documented trade-off: inside 30 s a consumed token may be retried; it cannot obtain tokens, and the legitimate winner remains active.

## 9. Failure recovery

The rollback test proves that an exception between consumption and replacement commit leaves the database consistent: the conditional update and the failed insert share one transaction, so the old session is **not** left consumed. Lost-response behavior remains the documented 409 → retry-once path.

## 10. Security stamp and revocation

- Stamp persists and rotates in SQL.
- Password/role/activation paths rotate the stamp and revoke sessions (unit/API tests from AUTH-01).
- Logout, logout-all, family revocation, and cascade deletion verified directly against SQL.
- Cross-process cache/state cannot undermine validation because validation reads the persisted stamp per request.

## 11. Cookie, CSRF, CORS, React

Unchanged by AUTH-01-POST; previously verified by the 514 database-free API tests (HttpOnly/SameSite/Path cookie, refresh token absent from JSON, CSRF required, 401/409 behavior, logout/logout-all, change-password session) and by frontend lint/build. No regression was introduced (no production source changed).

## 12. Regression results

| Suite | Result |
|---|---|
| `Application.UnitTests` | **945 passed, 0 failed, 0 skipped** |
| `API.IntegrationTests` (database-free filter) | **514 passed, 0 failed, 0 skipped** |
| AUTH SQL verification + concurrency | **12 passed, 0 failed** |
| Cleanup probe | **0** leftover test databases |

Frontend lint/build were not re-run because no frontend file changed in AUTH-01-POST (last verified in AUTH-01: both exit 0).

## 13. Defects

**No production defect was found.** Two new test assertions were corrected during authoring (auto-generated default-constraint name; Base64Url SHA-256 length is 43, not 64) and one family-reason expectation was aligned with the documented audit semantics. No production source was modified.

## 14. Remaining limitations

1. **Cross-process concurrency is NOT VERIFIED** (single process, independent contexts only).
2. Grace-window timing was not exercised against SQL with real elapsed time; unit tests use an injected clock.
3. The refresh flow was exercised at service + repository level against SQL, not through two full HTTP hosts.
4. `Infrastructure.IntegrationTests` was not run in full; only the AUTH-01 SQL suites were executed.
5. Deployment topology remains same-site; a cross-site deployment requires `SameSite=None; Secure`.

## 15. Files added

- `Test/Infrastructure.IntegrationTests/Auth/AuthSessionSqlVerificationTests.cs` (10 tests).
- `Test/Infrastructure.IntegrationTests/Auth/AuthSessionRefreshSqlConcurrencyTests.cs` (2 tests).
- This report; updates to the AUTH-01 audit, roadmap, testing standards, and execution log.

## 16. Safety confirmation

`Presentation (WPF)/` — untouched. `DVLDf` — never connected to, queried, migrated, or modified; it does not exist on the verified test instance. All test databases were created and removed by the ownership-verified fixture.
