# DVLD Current Project State Snapshot — Template

> **Snapshot ID:** `SNAPSHOT-YYYY-MM-DD-NNN`  
> **Supersedes Snapshot:** `SNAPSHOT-YYYY-MM-DD-MMM` | `None — Initial Baseline Snapshot`  
> **Last Updated:** `YYYY-MM-DDTHH:MM:SS+OFFSET`  
> **Project Identity:** Driver & Vehicle Licensing Department (DVLD) Management System  
> **Repository Root:** `D:\DVLD-WPF`  
> **Document Status:** `DRAFT` | `CURRENT SNAPSHOT`  
> **Repository Persistence Status:** `PREPARED IN CONVERSATION` | `PERSISTED IN REPOSITORY`  

---

## 1. Verification Overview & Baseline Constraints

<!--
Summarize the current verification posture of the repository.
Explicitly distinguish direct, newly executed verification from historical claims.
Apply explicit confidence labels:
- VERIFIED: Tested or confirmed in current session.
- REPORTED: Documented in past audits but unverified in this session.
- INFERRED: Derived from architecture patterns; awaiting proof.
- PENDING: Unverified assumptions or pending tests.
-->

- **Git State Verification:** `[VERIFIED | REPORTED | UNINSPECTED]` (Include branch and HEAD if verified).
- **Build & Test Verification:** `[VERIFIED | REPORTED | UNEXECUTED]` (Include test suite results if executed).
- **Database Inspection:** `[VERIFIED | REPORTED | UNCONNECTED]` (State DVLDf protection status).
- **Governance Alignment:** `[VERIFIED | PENDING REVIEW]` (Confirm ADR and Roadmap consistency).

---

## 2. Architecture and Technology Context

<!--
Document active components and stack boundaries.
Explicitly state that WPF is frozen and out of scope.
-->

- **Backend Architecture:** ASP.NET Core 8 Web API (`API/DVLD.Api`), Clean/Onion layering (`Domain`, `Application`, `Infrastructure`).
- **Persistence:** SQL Server, Entity Framework Core 8, UnitOfWork pattern.
- **Shared Contracts:** `DVLD.Contracts` (DTOs and enums).
- **Frontend Architecture:** React 19 Single Page Application (`ClientApp/`), TypeScript 6, Vite 8, Tailwind CSS.
- **Frozen Components:** `Presentation (WPF)/` is legacy, frozen, and strictly excluded from development.

---

## 3. Verified Completed Work & Milestones

<!--
List completed roadmap phases, resolved audits, and accepted milestones with evidence links.
-->

| Milestone / Deliverable | Status | Confidence | Evidence Reference |
|---|---|---|---|
| `<Milestone Name>` | `Completed` | `VERIFIED` \| `REPORTED` | `<File path or commit hash>` |

---

## 4. Active Workstreams & Backlog Priorities

<!--
List currently active tasks and upcoming roadmap priorities (P0, P1, P2, P3).
-->

- **Active Engineering Task:** `<Description of active task or investigation>`
- **Immediate Roadmap Backlog:**
  - **P0 Critical Integrity:** `<Work item ID and summary>`
  - **P1 Provisional Priority:** `<Work item ID and summary>`
  - **P2 Reliability & Observability:** `<Work item ID and summary>`
  - **P3 Architecture & CI:** `<Work item ID and summary>`

---

## 5. Architectural Decisions and Policy Invariants

<!--
Summarize accepted ADRs and invariant project policies.
-->

- **Segregation of Duties (`ADR-O01`):** `APPROVED — Model B (Explicit Permission Separation)`.
- **Detained License Replacement (`ADR-O02`):** `PROVISIONALLY SELECTED — OFFICIAL POLICY VERIFICATION REQUIRED`.
  - *Boundary:* Internal project simulation rule; not confirmed Jordanian law. Implementation remains not authorized.
- **Refresh Token & Multi-Session (`SEC-05`):**
  - *Approved Design Baseline:* Access token 15m (memory only), Refresh token idle 7d, Absolute session 30d, HttpOnly cookie, Secure/HTTPS mandatory in production, SameSite Lax, SQL Server transactional audit.
  - *Unresolved Decisions:* Option A vs Option C unselected; Policy 1 vs Policy 2 undecided; Recovery duration ($T_{\text{recovery}}$) undecided (neither 15s nor 30s approved); Physical retention/cleanup unresolved.
- **Logging Boundaries:** Business Audit (`AUD-01` in SQL transaction), Security Events (`SEC-04` out-of-band), Diagnostic Telemetry (`OBS-03` via Serilog).

---

## 6. Authorization Boundaries & Phase Statuses

<!--
Document exact implementation authorization status for upcoming roadmap phases.
-->

| Phase | Scope Summary | Documented Status | Authorization Prerequisite |
|---|---|---|---|
| **Phase 12.2** | P0 & P1 Remediation (`INT-01`, `INT-02`, `PERF-01`) | `PROPOSED / NOT AUTHORIZED` | Explicit human review and authorization of Phase 12.2 scope. |
| **Phase 12.3** | P2 Observability & Audit (`OBS-01`, `OBS-03`, `AUD-01`, `INT-03`) | `PLANNED / NOT AUTHORIZED` | Phase 12.2 completion; NuGet & DB migration approvals; ADR-O02 verification. |
| **Phase 12.4** | Session Management & Frontend (`SEC-05`, `FE-01`, `TEST-02`) | `PLANNED / NOT AUTHORIZED` | Phase 12.3 completion; DB migration approval for session/token tables. |

> [!CAUTION]
> **Governance Invariant:** Inclusion on the roadmap does not constitute implementation authorization. Each phase requires separate explicit human authorization.

---

## 7. Risks, Blockers, and Constraints

<!--
Catalog known project risks, safety constraints, and unresolved dependencies.
-->

1. **Database Protection:** Zero interaction with `DVLDf`. Tests run strictly against dynamic `DVLD_IntegrationTests_<GUID>`.
2. **Policy Verification Blockers:** `<e.g., ADR-O02 department policy verification required>`.
3. **Unresolved Technical Decisions:** `<e.g., SEC-05 Option A vs C, Policy 1 vs 2>`.
4. **Frozen Codebases:** Zero modifications permitted to WPF client.

---

## 8. Recommended Next Steps

<!--
Specify the concrete, evidence-backed next action recommended to the Project Owner.
-->

1. `<Immediate recommended human review or approval step>`
2. `<Follow-on engineering or scoping step>`

---

## 9. Verification Records & Change History

<!--
Provide history of changes between snapshots and record references.
-->

### 9.1 Snapshot Change Log
- `SNAPSHOT-YYYY-MM-DD-NNN`: `<Summary of updates compared to superseded snapshot>`

### 9.2 Verification Record Summary
- `<Component / Area>`: `<Status and verification evidence>`

---

## 10. Snapshot Limitations & Integrity Statement

- **Snapshot Author:** `<Engineer or AI Agent Role>`
- **Verification Confidence:** `<Overall confidence statement>`
- **Integrity Notice:** This snapshot is a point-in-time historical document. It must not be silently rewritten. Subsequent state changes must be recorded in a newly incremented snapshot.
