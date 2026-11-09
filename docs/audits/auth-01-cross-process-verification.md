# AUTH-01-CROSS-PROCESS — Multi-Process Refresh Rotation Verification

> **Date:** 2026-11-09
> **Milestone:** AUTH-01-CROSS-PROCESS
> **Environment:** isolated LocalDB instance `<machine-redacted>\LOCALDB#224BCC1F|LOCALDB#224BCC1F`; generated `DVLD_IntegrationTests_Api_<GUID>` database only.
> **WPF:** untouched. **`DVLDf`:** never accessed.

---

## 1. Status

**VERIFIED for the executable scope.** Two independent API OS processes, each with its own service provider, DbContext, repositories and UnitOfWork, shared one isolated SQL database. Concurrent refresh of the same token produced exactly **one 200** and **one 409**; the database contained exactly two generations (consumed + one replacement) with one active. Reuse after the grace window revoked the family for both processes.

**Lost-response simulation and process-termination fault injection were not reproduced** (see limitations).

## 2. Topology

- Two real `dotnet exec DVLD.Api.dll` processes, separate OS processes, separate ports.
- Shared database created/migrated by `SharedSqlDatabaseFixture` using the ownership-verified `SqlServerTestEnvironment`.
- Test database prefix `DVLD_IntegrationTests_Api_<GUID>`; the API test-host guard accepted it.
- Each host configured with `ASPNETCORE_ENVIRONMENT=Testing`, the shared connection string, a test JWT key, and `AuthSession__RotationGraceSeconds` (30 for the race test, 0 for the grace/reuse test).
- No static state, in-memory lock, or shared service provider is used by the service code; only the SQL row lock/conditional update decides the winner.

## 3. Test 1 — concurrent rotation across processes

`ConcurrentRefresh_AcrossTwoApiProcesses_ExactlyOneRotationWins`

- Seeded one user and one active session (absolute expiry = now + 10 days).
- Both hosts presented the same raw token simultaneously (gate-synchronized).
- Results: one **200** with a new access token and a rotated `dvld_rt` cookie; one **409**; no refresh token in any JSON body.
- Database after: 2 family rows, exactly 1 active; consumed row has `RevocationReason = Rotated` and `ReplacedBySessionId = replacement.Id`; replacement retains the original absolute expiry.
- Proves: single-winner rotation across processes, no second active chain, absolute-expiry preservation.

## 4. Test 2 — reuse after grace across processes

`ReuseAfterGrace_AcrossProcesses_RevokesFamilyForBothHosts`

- Host A rotated the token (grace configured to 0 s for this pair).
- Host B replayed the consumed token → **401**.
- Host A then presented the legitimate replacement → **401** as well (family revoked).
- Database: both generations revoked; at least one `ReuseDetected`.
- Proves: revocation is persisted and immediately visible to the other process; no process-local cache can keep a revoked session usable.

## 5. What the existing single-process tests proved (and did not)

`AuthSessionRefreshSqlConcurrencyTests` used independent DbContexts and UnitOfWork instances but one OS process. That proves SQL conditional-update atomicity across transactions/connections, **not** cross-process behavior. This milestone closes that gap with actual API processes.

## 6. Lost response, termination, and DB interruption

Not reproduced. The documented contract remains: a 409 means the client retries once with the (now rotated) cookie; the winner's response/cookie must reach the browser. The server-side rotation is committed atomically before the response, so a lost response cannot create a partial chain — it can only leave the client holding the consumed token, which yields 409 inside grace and 401 after family revocation.

Process-kill-before/after-commit and mid-transaction connection interruption were not exercised; SQL rollback semantics are already covered by `FailedReplacementInsert_RollsBackConsumption` in AUTH-01-POST.

## 7. Environment and safety

- Instance identity was re-read at runtime; the LocalDB hash changed between sessions, confirming the CI fix's requirement to query identity rather than hardcode it.
- Only the generated test database was used; the fixture deleted it on disposal.
- No connection string, token, or secret is logged by the tests.

## 8. Limitations

1. Azure-hosted pipeline execution of CI-SQL-01 remains pending; this milestone used the locally verified provisioning path with the same scripts.
2. Lost HTTP response/abort and process termination were not reproduced end-to-end.
3. Frontend behavior was not re-exercised (unchanged).

## 9. Gate status

| Gate 2 requirement | Status |
|---|---|
| Two distinct OS processes | **VERIFIED** |
| Shared isolated SQL database | **VERIFIED** |
| Independent service providers/contexts | **VERIFIED** |
| Exactly one successful rotation | **VERIFIED** |
| One active replacement | **VERIFIED** |
| No duplicate active chain | **VERIFIED** |
| Cross-process reuse detection | **VERIFIED** |
| Lost-response recovery | **NOT VERIFIED** |
| No static locks | **VERIFIED** (by inspection + test) |
| No raw tokens in logs | **VERIFIED** (by inspection) |

**Gate 2: PARTIALLY VERIFIED** — core rotation/reuse guarantees proven; lost-response and termination fault injection outstanding.
