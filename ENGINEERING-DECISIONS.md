# Architecture Decision Records (ADR) Register

> **Authority Level:** Authoritative Architectural & Technical Decision Register  
> **Parent Document:** [`AGENTS.md`](./AGENTS.md)  
> **Repository Root:** `D:\DVLD-WPF`  
> **Baseline Commit:** `29e43d6ce26ff6e7f516c45f4f475b10bdc2bfd3`  

---

## 1. Decision Framework & Taxonomy

This register records the historical, active, proposed, rejected, and open architectural decisions governing the DVLD repository. All modifications to architecture, technology selection, or security boundaries must be submitted as Architecture Decision Records (ADRs).

Decisions are classified using explicit statuses:
- **ACCEPTED:** Actively implemented, validated in code, and recognized as the architectural standard.
- **APPROVED:** Formally approved business/architectural decision awaiting implementation authorization.
- **CONSTRAINT:** A non-negotiable operational or legacy boundary enforced on the project.
- **PROPOSED:** An engineered recommendation scheduled in the roadmap but not yet authorized or implemented.
- **REJECTED:** Demonstrably evaluated and rejected due to adverse performance, complexity, or flawed premises.
- **OPEN BUSINESS DECISION:** A business policy or legal question requiring formal stakeholder sign-off before technical execution.

---

## 2. ADR Summary Matrix

| ADR ID | Title | Status | Category | Scope |
|---|---|---|---|---|
| **ADR-001** | Layered Onion Architecture with UnitOfWork State Machine | **ACCEPTED** | Core Architecture | Backend (`API`, `App`, `Infra`, `Domain`) |
| **ADR-002** | Independent React SPA Client with Centralized Transport | **ACCEPTED** | Frontend Architecture | `ClientApp/` |
| **ADR-003** | JWT Bearer Token Authentication with Dynamic DB Revocation | **ACCEPTED** | Security Architecture | `DVLD.Api`, `Program.cs` |
| **ADR-004** | Distributed Slot Mutual Exclusion via `sp_getapplock` | **ACCEPTED** | Concurrency Architecture | `Infrastructure`, SQL Server |
| **ADR-005** | Functional `Result<T>` Modeling over Domain Exceptions | **ACCEPTED** | Design Pattern | `Application`, Controllers |
| **ADR-C01** | Exclusion and Freeze of Legacy WPF Desktop Client | **CONSTRAINT** | Project Scope | `Presentation (WPF)/` |
| **ADR-C02** | Isolation of Production Database (`DVLDf`) from Automated Testing | **CONSTRAINT** | Database Safety | All Test Suites, CI |
| **ADR-P01** | Separation of Non-Locking Reads from Update-Locking Queries (PERF-02) | **PROPOSED** | Performance / Concurrency | `LicenseRepository.cs` |
| **ADR-P02** | Standardized RFC 7807 ProblemDetails across All Endpoints (REST-01) | **PROPOSED** | API Standards | All Controllers, Error Handlers |
| **ADR-P03** | Deletion of Orphaned Direct Completion Endpoint (INT-01) | **PROPOSED** | Domain Integrity | `ApplicationsController.cs` |
| **ADR-R01** | Rejection of `Task.Run` for In-Process CPU Work (PERF-03) | **REJECTED** | Concurrency | `Application Services` |
| **ADR-R02** | Rejection of Multi-Tenant IDOR Claim on Citizen Photos (SEC-02) | **REJECTED** | Security | `PeopleController.cs` |
| **ADR-R03** | Rejection of Pagination on Tiny Static Reference Tables (API-02) | **REJECTED** | API Design | Reference Controllers |
| **ADR-O01** | Retention vs. Relaxation of Segregation of Duties (SEC-01) | **APPROVED** | Security Policy | Authorization Policies |
| **ADR-O02** | Detained License Replacement Policy (INT-03) | **PROVISIONALLY SELECTED — OFFICIAL POLICY VERIFICATION REQUIRED** | Business Rule | `LicenseReplacementService.cs` |

---

## 3. Accepted Architecture Decisions

### ADR-001: Layered Onion Architecture with UnitOfWork State Machine
- **Status:** `ACCEPTED`
- **Context:** The system coordinates high-stakes legal licensing workflows involving citizens, tests, fees, and driving credentials requiring coordinated multi-entity persistence.
- **Decision:** Implement Onion Architecture with explicit directional dependency rules:
  - `Domain` is independent and has zero dependencies.
  - `Application` depends only on `Domain`.
  - `Infrastructure` depends on `Application` (implementing interfaces) and `Domain` (mapping entities).
  - `DVLD.Api` acts as composition root, referencing `Application`, `Infrastructure`, and `DVLD.Contracts`.
  - `Application` must **never** depend on `Infrastructure`.
  - Encapsulate transactional data access within repositories governed by a Scoped `UnitOfWork` implementing deferred physical commits, ambiguous commit verification, and execution strategy retries.
- **Architectural Scope & Atomicity Boundaries:**
  - Atomicity is provided within the boundary of an active database transaction. Achieving atomicity depends on defining correct transaction boundaries, ensuring all participating write operations enlist in the same transaction, and verifying that commit/rollback behaviors succeed.
  - Unit of Work does **not** provide an absolute guarantee against all partial writes under every catastrophic failure (e.g., partial external filesystem side-effects during photo uploads, or unhandled process crashes prior to rollback).
  - On handled failure results and caught exceptions, explicit rollback and `ChangeTracker.Clear()` prevent stale or corrupted EF Core tracker state from leaking into subsequent requests.

### ADR-002: Independent React SPA Client with Centralized Transport
- **Status:** `ACCEPTED`
- **Context:** The legacy WPF client needed modernization for web access without disrupting existing desktop operations.
- **Decision:** Build an independent React 19 + TypeScript + Vite single-page application (`ClientApp/`) communicating over HTTP + JSON + JWT. Route all requests through a centralized `apiClient.ts` with automated 401 session interception.
- **Consequences:** Clean separation of concerns. Frontend state is isolated; backend API remains the sole authority.

### ADR-003: JWT Bearer Token Authentication with Dynamic DB Revocation
- **Status:** `ACCEPTED`
- **Context:** Stateless JWT tokens cannot natively be invalidated before expiration without distributed cache infrastructure.
- **Decision:** Implement `JwtBearerEvents.OnTokenValidated` to verify `IUserService.IsUserActiveAsync(userId)` directly in SQL Server on every authenticated request.
- **Consequences:** Provides immediate user deactivation capability without managing Redis blacklists or distributed state.

### ADR-004: Distributed Slot Mutual Exclusion via `sp_getapplock`
- **Status:** `ACCEPTED`
- **Context:** Test appointment scheduling experiences race conditions on test slot capacities that table-level locks or serializable isolation cannot prevent without serializing the entire table.
- **Decision:** Utilize SQL Server's `sp_getapplock` with exclusive transaction locks on deterministic slot keys (`DVLD_Slot_{testTypeId}_{dateTime}`).
- **Required Conditions for Concurrency Protection:**
  - `sp_getapplock` provides effective mutual exclusion only when all following conditions are satisfied:
    1. **Consistent Resource Naming:** Every scheduling and rescheduling path must generate identical, deterministic lock keys.
    2. **Correct Lock Scope & Ownership:** `@LockOwner` must be set to `'Transaction'` and run inside an active transaction.
    3. **Explicit Acquisition Checking:** Return codes must be validated (return value `>= 0` indicates successful acquisition; `< 0` indicates timeout or deadlock).
    4. **Transaction Lifetime Alignment:** The transaction must remain open until all slot validation and appointment insert statements are staged.
    5. **Universal Protocol Adherence:** All code paths that reserve or modify slot capacity must adhere to this exact lock protocol; any path bypassing the lock protocol can cause race conditions.
  - Limitations: Lock acquisition timeouts and deadlocks must be caught and converted to clean failure outcomes rather than unhandled server faults.

### ADR-005: Functional `Result<T>` Modeling over Domain Exceptions
- **Status:** `ACCEPTED`
- **Context:** Using exceptions for control flow (e.g., "license expired", "person not found") introduces heavy stack-trace overhead and obscures API contracts.
- **Decision:** Use explicit `Result` and `Result<T>` return types for all Application Service workflows.
- **Consequences:** Clear, predictable execution paths. Controllers cleanly map failure results to HTTP 400, 404, or 409 status codes.

---

## 4. Architectural Constraints

### ADR-C01: Exclusion and Freeze of Legacy WPF Desktop Client
- **Status:** `CONSTRAINT`
- **Context:** The repository contains a legacy WPF desktop client in `Presentation (WPF)/`.
- **Constraint:** The WPF application is **frozen** and explicitly out of scope for the current engineering and remediation roadmap. AI agents and engineers must not refactor, audit, or rewrite WPF code unless explicitly commissioned.

### ADR-C02: Isolation of Production Database (`DVLDf`) from Automated Testing
- **Status:** `CONSTRAINT`
- **Context:** The primary development database is named `DVLDf`.
- **Constraint:** Automated integration tests are strictly prohibited from connecting to, dropping, or mutating `DVLDf`. All integration tests must target ephemeral `DVLD_IntegrationTests_<GUID>` databases configured via `DVLD_TEST_CONNECTION`.

---

## 5. Proposed Architecture Decisions (Pending Phase 12 Execution)

### ADR-P01: Separation of Non-Locking Reads from Update-Locking Queries (PERF-02)
- **Status:** `PROPOSED`
- **Context:** `LicenseRepository.GetLicenseByIdAsync` currently hardcodes `WITH (UPDLOCK, HOLDLOCK)`, serializing read-only queries against concurrent updates and other readers.
- **Proposal:** Refactor `GetLicenseByIdAsync` to execute ordinary shared reads. Introduce a dedicated `GetForUpdateAsync` method exclusively for write workflows.
- **Consequences:** Eliminates unnecessary reader serialization.

### ADR-P02: Standardized RFC 7807 ProblemDetails across All Endpoints (REST-01)
- **Status:** `PROPOSED`
- **Context:** `AuthController` returns ad-hoc `{ error = "..." }` schemas, while `GlobalExceptionHandler` returns standard `ProblemDetails`.
- **Proposal:** Standardize all controller failure outcomes on RFC 7807 `ProblemDetails`.
- **Consequences:** Simplifies client-side error parsing in `ClientApp`.

### ADR-P03: Deletion of Orphaned Direct Completion Endpoint (INT-01)
- **Status:** `PROPOSED`
- **Context:** `POST /api/applications/{id}/complete` allows unauthorized, direct mutation of applications to `Completed` without prerequisites.
- **Proposal:** Delete endpoint and underlying service method.
- **Consequences & Approval Prerequisite:** Removing or altering a public API endpoint represents a potentially breaking API contract change. **Explicit Human Engineering Approval is strictly mandatory** before deleting the endpoint, altering its contract, or removing its underlying service method. Closes critical data corruption vulnerability once approved.

---

## 6. Rejected Architecture Recommendations

### ADR-R01: Rejection of `Task.Run` for In-Process CPU Work (PERF-03)
- **Status:** `REJECTED`
- **Proposal:** Wrap BCrypt hashing and SkiaSharp image resizing inside `Task.Run()` to "offload work from request threads".
- **Rejection Rational:** In ASP.NET Core, HTTP requests already execute on ThreadPool worker threads. Calling `Task.Run()` queues another delegate to the same ThreadPool, incurring context switching, allocation overhead, and thread pool starvation under load.
- **Decision:** Execute CPU-bound computation synchronously on the active request thread.

### ADR-R02: Rejection of Multi-Tenant IDOR Claim on Citizen Photos (SEC-02)
- **Status:** `REJECTED`
- **Proposal:** Treat photo retrieval without user-ownership checks as an Insecure Direct Object Reference (IDOR) vulnerability.
- **Rejection Rationale:** DVLD is an internal government department portal. Authenticated actors are staff members managing public citizen records. Citizens have no logins or resource ownership.
- **Decision:** Retain role-based access control and audit logging; reject the multi-tenant IDOR premise.

### ADR-R03: Rejection of Pagination on Tiny Static Reference Tables (API-02)
- **Status:** `REJECTED (INFORMATIONAL)`
- **Proposal:** Mandate pagination on reference data endpoints (`ApplicationTypes`, `TestTypes`, `Countries`).
- **Rejection Rationale:** These tables hold small, static datasets (7 application types, 3 test types, 195 countries) that rarely change. Paginating them introduces client overhead with zero database performance gain.
- **Decision:** Retain unpaged cached GET endpoints for small static lookups.

---

## 7. Business Decisions & Policy Records (ADR-O01 & ADR-O02)

### ADR-O01: Retention vs. Relaxation of Segregation of Duties (SEC-01)
- **Status:** `APPROVED` (Decision Date: 2026-11-08)
- **Context:** The API enforces `[Authorize(Policy = "StaffOnly")]` on operational licensing endpoints (`Applications`, `Licenses`, `Detentions`). Administrators (`Admin`) attempting counter operations receive HTTP 403 Forbidden.
- **Historical Options Evaluated:**
  - *Model A (Role Hierarchy):* Relax `StaffOnly` to `StaffOrAdmin`, granting administrators superuser operational rights.
  - *Model B (Explicit Permission Separation - Selected):* Maintain strict separation between Administrative system management and Staff counter operations to prevent internal fraud.
- **Human Decision Record (2026-11-08):**
  - **Selected Model:** **Model B — Explicit Permission Separation**.
  - **Decisions Enforced:**
    - Admin does not automatically inherit Staff operational permissions.
    - Staff does not receive administrative user-management permissions.
    - Shared capabilities must be explicitly defined and authorized.
    - Any future exception or permission expansion requires separate documented approval.
    - The existing authorization model must remain unchanged unless a separately authorized implementation phase is approved.
- **Implementation Authorization:**
  - **Phase 12.2 remains NOT AUTHORIZED.**
  - No source-code changes are authorized.
  - No test modifications are authorized.
  - No database or schema changes are authorized.
  - A separate explicit human authorization is required before implementation.

### ADR-O02: Detained License Replacement Policy (INT-03)
- **Status:** `PROVISIONALLY SELECTED — OFFICIAL POLICY VERIFICATION REQUIRED` (Provisional Direction Date: 2026-11-08)
- **Context:** Static code inspection confirms that `LicenseReplacementService` does not check whether a license is detained before issuing a replacement.
- **Provisionally Selected Business Rule:**
  1. A driving license with an active detention must not be replaced.
  2. The active detention must first be resolved through the existing Release Detained License workflow.
  3. Once the detention is released, replacement may proceed if all other replacement business validations are satisfied.
  4. Historical detention and release records must remain preserved. Replacing a license must not erase or rewrite historical detention records.
  - **Purpose:** Maintain consistency between the detention workflow and the license replacement workflow and prevent replacement from bypassing an active detention.
- **Important Scope Clarification:**
  - This is a project-specific business rule.
  - The project simulates core licensing operations of a Driving and Vehicle Licensing Department.
  - Government paperwork, official evidence, external authority correspondence, temporary permits, and other real-world administrative procedures are outside the project's intended scope unless explicitly introduced later.
  - Do not describe this decision as a confirmed legal requirement or official Jordanian procedure. Official policy verification remains required.
- **Technical Questions — Not Yet Decided:**
  The following implementation details are not yet decided and must not be invented or assumed:
  - Is detention associated with `LicenseId`, `DriverId`, or both?
  - How should the system identify the active detention when validating a replacement?
  - What historical relationships must be preserved when a replacement creates a new license record?
  - Lost-license and damaged-license replacement procedures must not be assumed to have identical requirements.
  - What official policy verification is still required?
  - *Mandatory Investigation Gate:* These remain technical questions requiring inspection of the existing Domain model, repositories, and workflows before implementation. Do not change the data model or implementation in this phase.
- **Implementation Authorization:**
  - **Phase 12.2 remains NOT AUTHORIZED.**
  - No source-code changes are authorized.
  - No test modifications are authorized.
  - No database or schema changes are authorized.
  - A separate explicit human authorization is required before implementation.
