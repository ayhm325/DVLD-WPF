# AI Agent Engineering Instructions — DVLD Cinematic Login

> **Authority Level:** Mandatory Repository Instruction  
> **Target System:** Google Antigravity / AI Pair Programming Sessions  
> **Project Root:** `D:\DVLD-WPF\ClientApp`  
> **Governance Status:** ACTIVE & ENFORCED

---

## 1. Overview & Purpose

This file governs all AI coding sessions working on the DVLD (Driving & Vehicle Licensing Department) Cinematic Login project.

The goal of this initiative is to transform the DVLD login page into a photorealistic, cinematic nighttime 3D architectural visualization matching the approved visual reference image, while strictly preserving authentication integrity, existing application logic, and repository safety.

Any AI agent operating in this repository **must** follow the protocols and boundaries defined in this document.

---

## 2. Rendering Architecture Overview

To prevent inaccurate assumptions, all agents must recognize the distinct implementation layers currently operating in the codebase:

1. **3D Scene Environment (WebGL / Three.js / React Three Fiber):**
   - Implemented in `src/components/login/scene/`.
   - Renders the modern headquarters building (`HeadquartersModel.tsx`), the Amman Citadel ruins (`CitadelModel.tsx`), directional architectural lights, radiosity PMREM environment, asphalt ground plane, and vehicle placeholder.
2. **Interactive Authentication Interface (HTML / CSS):**
   - Implemented in `src/components/Login.tsx`, `Login.css`, and `LoginBranding.tsx`.
   - The glassmorphic login card, input fields, Remember Me toggle, and SSO buttons are standard HTML/CSS elements styled with Tailwind and CSS backdrop filters, rendered as a 2D presentation layer layered over the 3D canvas. They are **not** 3D WebGL geometries.
3. **Holographic Driving License Overlay (SVG / CSS):**
   - Implemented in `src/components/login/HolographicLicense.tsx`.
   - Currently rendered as a 2D SVG/CSS HUD component positioned above the building artwork. Projecting this asset as an in-scene 3D mesh/shader is a proposed future enhancement (Phase 06/08), not the current implementation.
4. **Resilient 2D Fallback Environment (SVG / CSS):**
   - Implemented in `src/components/login/CinematicEnvironment.tsx`.
   - Rendered automatically when WebGL is unavailable or when the WebGL canvas encounters an unhandled runtime error.

---

## 3. Mandatory Reading Order

Before beginning **any** task or phase, every AI agent must inspect documents in the following strict order:

1. [`AGENTS.md`](./AGENTS.md) — Operational instructions, architecture definitions, and non-negotiable governance rules (this document).
2. [`docs/cinematic-login/README.md`](./docs/cinematic-login/README.md) — Documentation index, navigation, and authority hierarchy.
3. [`docs/cinematic-login/00-PROJECT-CHARTER.md`](./docs/cinematic-login/00-PROJECT-CHARTER.md) — Approved project charter, visual requirements, and protected boundaries.
4. [`docs/cinematic-login/08-CURRENT-STATE.md`](./docs/cinematic-login/08-CURRENT-STATE.md) — Canonical current status, completed work, verified technical state, and next authorized actions.
5. [`docs/cinematic-login/07-ROADMAP.md`](./docs/cinematic-login/07-ROADMAP.md) — Complete 11-phase sequence, phase status, and prerequisites.
6. **Phase-Specific Documents** — Detailed specifications relevant to the task (e.g., `01-VISUAL-REFERENCE.md`, `03-COMPOSITION-SPEC.md`, `04-TECH-STACK.md`, `05-ARCHITECTURE.md`, `06-ASSET-REGISTER.md`, `10-QUALITY-GATES.md`).
7. **Decision Records** — [`docs/cinematic-login/09-DECISIONS.md`](./docs/cinematic-login/09-DECISIONS.md) to understand historical architectural and design choices.

> **Crucial Rule:** Never rely on chat history as the single source of project truth. Persistent markdown files in `docs/cinematic-login/` represent the project's living truth.

---

## 4. Standardized Status Categories

To ensure consistency across all documents, agents must strictly separate **Information/Decision Statuses** from **Phase Lifecycle Statuses**:

### 4.1 Information & Decision Statuses
Used in specifications, ADRs, asset registers, and technical audits:
- `VERIFIED` — Confirmed by codebase inspection, command execution, or asset analysis.
- `APPROVED` — Explicitly approved in writing by the user.
- `PROPOSED` — Suggested design, architecture, or threshold awaiting user review.
- `PENDING` — Awaiting a future phase or measurement.
- `UNKNOWN` — Information is missing; requires verification without guessing.
- `BLOCKED` — Cannot proceed without a prerequisite decision or external dependency.
- `DEFERRED` — Intentionally postponed to a later milestone.

### 4.2 Phase Lifecycle Statuses
Used exclusively in roadmap tracking and phase execution:
- `PLANNED` — Defined in roadmap; not yet authorized for work.
- `IN PROGRESS` — Explicitly authorized by the user and currently being executed.
- `TECHNICALLY VERIFIED` — Code, builds, lints, and documentation completed by the agent; awaiting user review.
- `VISUAL REVIEW` — Deliverables submitted with visual evidence for stakeholder inspection.
- `CHANGES REQUESTED` — User reviewed deliverables and requested targeted corrections.
- `APPROVED` — Explicitly approved in writing by the user; phase milestone sealed.

---

## 5. Mandatory Execution Rules

### 5.1 Scope & Phase Confinement
- **Single-Phase Execution:** Execute **only** the single phase explicitly authorized by the user.
- **No Scope Creep:** Never silently expand the scope beyond the explicit prompt instructions.
- **Stop Condition:** Upon completing the deliverables of the authorized phase, **STOP immediately** and await explicit user review and approval before advancing.

### 5.2 Working Tree & Git Safety
- **Inspect Before Editing:** Always run `git status --short` before touching any file.
- **Preserve Uncommitted Changes:** Pre-existing uncommitted modifications in the working tree must be preserved and never overwritten or reverted.
- **Zero Destructive Git Commands:** Do **not** commit, push, create branches, stash, reset, or checkout away from the working branch unless explicitly commanded by the user.

### 5.3 Technology & Dependency Discipline
- **Verify Installed Packages:** Inspect `package.json` and lockfiles directly. Never assume a package or version exists based on conversation history.
- **No Unapproved Packages:** Do **not** install, upgrade, downgrade, or remove dependencies without explicit prior approval from the user.
- **Documented Evaluations:** Any proposed dependency change must be evaluated via an ADR in `09-DECISIONS.md` before implementation.

### 5.4 Protected Application Boundaries
Unless explicitly authorized by the user in a designated authentication phase, the following subsystems are **STRICTLY OFF-LIMITS**:
- Authentication logic (`src/services/authService.ts`, `src/context/useAuth.ts`, etc.)
- Login form validation, submission behavior, and error handling contracts
- Token storage (`localStorage` auth tokens, session state)
- Backend APIs (`DVLD_API`), business layers (`DVLD_Business`), data access (`DVLD_DataAccess`), and database schemas
- WPF desktop client code (`DVLD_Presentation`)
- Routing and protected application pages (`DashboardPage`, `PeoplePage`, `LicensesPage`, etc.)

### 5.5 Factual Integrity & Validation
- **No Hallucinated Data:** Never invent test results, visual coordinates, render timings, asset licenses, or user approvals.
- **No Absolute Guarantees:** Do not make unverifiable absolute claims (such as "100% device compatibility" or "zero-overhead"). Frame criteria around measurable tests and documented fallback behaviors.
- **Label Unknowns Clearly:** If information is missing or unverified, label it explicitly (e.g., `UNKNOWN — VERIFICATION REQUIRED` or `PRELIMINARY ESTIMATE — PENDING PHASE 01`).

---

## 6. Conflict Resolution Protocol

If a user request conflicts with the approved Project Charter or attempts to modify a protected boundary:
1. **Identify the conflict:** Clearly cite the conflicting rule or boundary from `00-PROJECT-CHARTER.md`.
2. **Explain the impact:** State the architectural, functional, or visual consequence of the requested change.
3. **Propose a safe alternative:** Offer a non-destructive alternative that accomplishes the goal within governance rules.
4. **Request explicit authorization:** Ask the user to confirm whether they intend to override the charter rule before executing.

---

## 7. Phase Completion & Documentation Hand-Off

At the conclusion of every authorized phase, the agent must:
1. Update [`docs/cinematic-login/08-CURRENT-STATE.md`](./docs/cinematic-login/08-CURRENT-STATE.md) with exact files created, modified, verified status, and next authorized actions.
2. Update [`docs/cinematic-login/07-ROADMAP.md`](./docs/cinematic-login/07-ROADMAP.md) reflecting the new phase lifecycle status (`TECHNICALLY VERIFIED`, `CHANGES REQUESTED`, etc.).
3. Update [`docs/cinematic-login/06-ASSET-REGISTER.md`](./docs/cinematic-login/06-ASSET-REGISTER.md) if models, textures, or media were introduced or modified.
4. Record any architectural or design decisions in [`docs/cinematic-login/09-DECISIONS.md`](./docs/cinematic-login/09-DECISIONS.md), ensuring approval evidence is factual and not assumed.
5. Run non-destructive baseline checks (`npm.cmd --prefix ClientApp run build` and `npm.cmd --prefix ClientApp run lint`).
6. Present the standard phase completion report and **STOP**.
