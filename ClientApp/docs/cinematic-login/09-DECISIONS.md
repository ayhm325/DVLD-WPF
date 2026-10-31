# DVLD Cinematic Login — Architecture & Design Decision Records (ADR)

> **Document ID:** `09-DECISIONS`  
> **Status:** ACTIVE DECISION LOG  
> **Authoritative For:** Historical Architectural Choices, Evaluated Alternatives, and Trade-offs

---

## Decision Record Index

| ADR ID | Title | Date | Status | Category | Approval Evidence |
|---|---|---|---|---|---|
| `ADR-001` | Reference Image as Definitive Visual Authority | 2026-10-31 | `APPROVED` | Visual Direction | Explicitly mandated in User Prompt Project Charter Rule 1 |
| `ADR-002` | Three.js + React Three Fiber as Interactive 3D Engine | 2026-10-31 | `VERIFIED IMPLEMENTATION — POLICY PROPOSED` | Rendering Technology | Verified in source code; formal policy proposed for sign-off |
| `ADR-003` | Client-Side WebGL Detection with SVG/CSS Fallback | 2026-10-31 | `VERIFIED IMPLEMENTATION — POLICY PROPOSED` | Reliability & Resilience | Verified in source code; formal policy proposed for sign-off |
| `ADR-004` | Strict Sequential Phase-Gate Governance Model | 2026-10-31 | `APPROVED` | Process & Governance | Explicitly mandated in User Prompt Project Charter Rule 3 |
| `ADR-005` | Isolation of Protected Application & Auth Boundaries | 2026-10-31 | `APPROVED` | Security & Stability | Explicitly mandated in User Prompt Project Charter Rule 4 |
| `ADR-006` | Mandatory Preservation of Holographic Driving License | 2026-10-31 | `APPROVED` | Visual Identity | Explicitly mandated in User Prompt Project Charter Rule 1 |
| `ADR-007` | Master 2D Normalized Composition Coordinate Schema | 2026-10-31 | `APPROVED` | Spatial Specification | Verified reference baseline 1024x512 established in Phase 01 |

---

## Detailed Decision Records

### Decision ID: ADR-001
- **Title:** Reference Image as Definitive Visual Authority
- **Date:** 2026-10-31
- **Status:** `APPROVED`
- **Context:** Previous design passes drifted between generic modern dashboard concepts and isolated flat SVG illustrations.
- **Problem:** AI coding assistants lacked a single definitive visual standard, resulting in ungrounded visual churn.
- **Options Considered:**
  1. Allow AI artistic flexibility to improvise modern government login layouts.
  2. Adopt a generic modern glassmorphism dashboard template.
  3. Establish the stakeholder-provided cinematic reference image as the absolute, non-negotiable visual target.
- **Decision:** Option 3 is adopted. The supplied reference image is the primary visual authority for all architectural, lighting, spatial, and styling decisions.
- **Reasoning:** A fixed, verified visual reference eliminates ambiguity, enforces high standards, and guarantees visual coherence.
- **Advantages:** Consistent target across multiple AI sessions; clear acceptance criteria; alignment with stakeholder vision.
- **Disadvantages:** Demands high fidelity and photorealistic asset production techniques.
- **Risks:** Difficulty matching complex lighting without excessive GPU consumption; mitigated by careful PBR material optimization.
- **Affected Components:** All visual scene components, camera, lighting, and login card styling.
- **Approval Evidence:** Explicitly mandated in User Prompt Project Charter (Rule 1: "The supplied DVLD cinematic login reference image is the primary visual authority").

---

### Decision ID: ADR-002
- **Title:** Three.js + React Three Fiber as Interactive 3D Engine
- **Date:** 2026-10-31
- **Status:** `VERIFIED IMPLEMENTATION — POLICY PROPOSED`
- **Context:** The login experience requires realistic architectural depth, specular wet road reflections, and camera dynamics that flat 2D SVGs cannot achieve.
- **Problem:** Need a high-performance, web-standard 3D rendering pipeline that integrates declaratively with React 19 without third-party iframe embeds.
- **Options Considered:**
  1. Canvas 2D procedural rendering with layered PNG cutouts.
  2. Pure WebGL custom pipeline written from scratch.
  3. Three.js (`^0.187.0`) paired with React Three Fiber (`^9.8.2`) and Drei (`^10.7.9`).
  4. Pre-rendered MP4 video background.
- **Decision:** Three.js + React Three Fiber is the current verified implementation in `ClientApp/package.json` and `src/components/login/scene/`. Adopting this stack as the permanent project policy is `PROPOSED` for formal user approval upon Phase 00.1 sign-off.
- **Reasoning:** Standardized, battle-tested, supports PBR materials, ACESFilmic tone-mapping, soft shadows, and GLTF 2.0 loading. Pre-rendered video was rejected due to lack of responsiveness and inability to support dynamic resolution scaling.
- **Advantages:** Declarative JSX scene graph, hardware acceleration, full control over lights/camera, seamless integration into React 19.
- **Disadvantages:** WebGL hardware dependency on client machines.
- **Risks:** Device GPU limitations; mitigated by strict LOD management and ADR-003 fallback mechanism.
- **Affected Components:** `Login3DScene.tsx`, `SceneCanvas.tsx`, `SceneCamera.tsx`, `SceneLighting.tsx`, model loaders.
- **Approval Evidence:** Current technical implementation verified in `package.json` and `SceneCanvas.tsx`. Formal architectural policy proposed for explicit user approval upon Phase 00.1 sign-off.

---

### Decision ID: ADR-003
- **Title:** Client-Side WebGL Detection with SVG/CSS Fallback
- **Date:** 2026-10-31
- **Status:** `VERIFIED IMPLEMENTATION — POLICY PROPOSED`
- **Context:** Government administrative workstations, remote desktop sessions (RDP/Citrix), and legacy office hardware may lack WebGL2 acceleration or block WebGL contexts.
- **Problem:** If a 3D scene crashes or fails to initialize, users must never be blocked from logging into the DVLD portal.
- **Options Considered:**
  1. Show a generic error message ("WebGL not supported").
  2. Fall back to a plain solid dark background.
  3. Automatically detect WebGL capability and wrap 3D canvas in a React Error Boundary that renders a procedural SVG/CSS environment (`CinematicEnvironment.tsx`).
- **Decision:** Synchronous WebGL capability detection combined with React Error Boundary fallback is the current verified implementation in `Login3DScene.tsx`. Adopting this as permanent project policy is `PROPOSED` for formal user approval upon Phase 00.1 sign-off.
- **Reasoning:** Ensures authentication availability regardless of client graphics hardware or GPU driver failures.
- **Advantages:** Graceful degradation; prevents unhandled WebGL exceptions from crashing the authentication form.
- **Disadvantages:** Requires maintaining a dual-mode visual presentation (3D scene + 2D SVG fallback).
- **Risks:** Divergence between 3D scene and 2D fallback; mitigated by keeping SVG fallback stable and self-contained.
- **Affected Components:** `Login3DScene.tsx`, `SceneErrorBoundary`, `CinematicEnvironment.tsx`.
- **Approval Evidence:** Current technical implementation verified in `Login3DScene.tsx`. Formal architectural policy proposed for explicit user approval upon Phase 00.1 sign-off.

---

### Decision ID: ADR-004
- **Title:** Strict Sequential Phase-Gate Governance Model
- **Date:** 2026-10-31
- **Status:** `APPROVED`
- **Context:** Previous phases suffered from uncontrolled scope expansion, attempting to modify UI, architecture, materials, and models simultaneously in single turns.
- **Problem:** Multi-faceted changes prevented rigorous visual reviews and introduced regression risks.
- **Options Considered:**
  1. Freeform task progression driven solely by conversational momentum.
  2. Formal sequential lifecycle (`Planned -> In Progress -> Technically Verified -> Visual Review -> Changes Requested -> Approved`).
- **Decision:** Option 2 is adopted.
- **Reasoning:** Enforces discipline, traceability, verifiable validation gates, and prevents AI agents from executing unapproved work.
- **Advantages:** Predictable deliverables; clear boundaries; zero accidental feature bloat.
- **Disadvantages:** Requires formal user approval at every phase boundary.
- **Risks:** Waiting latency between phases; mitigated by comprehensive, self-contained phase reports.
- **Affected Components:** Project governance, roadmap tracking, phase prompt templates.
- **Approval Evidence:** Explicitly mandated in User Prompt Project Charter (Rule 3: "Strict Phase Approval").

---

### Decision ID: ADR-005
- **Title:** Isolation of Protected Application & Auth Boundaries
- **Date:** 2026-10-31
- **Status:** `APPROVED`
- **Context:** The login page connects to backend authentication services (`authService.ts`), JWT handling, and routing.
- **Problem:** Visual enhancements to the login screen must never compromise or modify authentication security, API contracts, or existing business logic.
- **Options Considered:**
  1. Allow refactoring of authentication logic while redesigning the login form.
  2. Strictly isolate visual components from authentication contracts, freezing API services, token management, and backend logic.
- **Decision:** Option 2 is adopted.
- **Reasoning:** Security and stability must never be compromised for visual aesthetics.
- **Advantages:** Complete safety of existing backend, database, and auth flows.
- **Disadvantages:** Form markup must strictly maintain existing state variables and submission hooks.
- **Risks:** None; eliminates regression surface.
- **Affected Components:** `src/services/authService.ts`, `src/context/useAuth.ts`, `Login.tsx` form handlers.
- **Approval Evidence:** Explicitly mandated in User Prompt Project Charter (Rule 4: "Application Functionality Must Remain Protected").

---

### Decision ID: ADR-006
- **Title:** Mandatory Preservation of Holographic Driving License
- **Date:** 2026-10-31
- **Status:** `APPROVED`
- **Context:** The reference concept features a futuristic floating holographic driving license projected above the headquarters.
- **Problem:** Prior sessions debated whether this element was decorative or core to the institutional theme.
- **Options Considered:**
  1. Remove the holographic license to create a conventional architectural photo.
  2. Retain the holographic Jordanian driving license card and emitter rings as an intentional, mandatory component of the design.
- **Decision:** Option 2 is adopted. The visual concept of the holographic license is a mandatory design requirement.
- **Reasoning:** The holographic card is the signature storytelling motif communicating DVLD's digital transformation mission.
- **Advantages:** Preserves unique visual identity and 1:1 fidelity with the approved reference image.
- **Disadvantages:** Requires complex holographic visual treatment.
- **Risks:** Visual collision with building facade or text; mitigated by strict spatial zoning in Phase 03.
- **Implementation Note:** The current implementation renders the holographic card as a 2D SVG overlay in the DOM (`HolographicLicense.tsx`). Transitioning this to an in-scene 3D WebGL projection is a `PROPOSED` enhancement for Phase 06/08.
- **Affected Components:** `HolographicLicense.tsx`, `Login3DContent.tsx`.
- **Approval Evidence:** Explicitly mandated in User Prompt Project Charter (Rule 1: "Preserve the holographic Jordanian driving license. It is an intentional part of the reference composition").

---

### Decision ID: ADR-007
- **Title:** Master 2D Normalized Composition Coordinate Schema
- **Date:** 2026-10-31
- **Status:** `APPROVED`
- **Context:** Translating a single 2D panoramic visual reference image into multi-resolution 3D scenes and responsive UI overlays requires an unambiguous coordinate measurement standard.
- **Problem:** Prior phases used qualitative descriptions or inconsistent pixel values from unverified viewports, causing visual drift.
- **Options Considered:**
  1. Measure elements in arbitrary CSS pixels based on whatever browser window is open.
  2. Attempt to reverse-engineer hypothetical 3D world meters from a 2D perspective image.
  3. Establish the verified 1024 x 512 reference pixel grid as the absolute master canvas, with normalized $[0.0, 1.0]$ and percentage $[0.0\%, 100.0\%]$ coordinates as the standard cross-phase exchange format.
- **Decision:** Option 3 is adopted. All spatial analyses, layout boundaries, bounding boxes, and clearances are defined relative to the 1024 x 512 master reference coordinate space and normalized formulas ($X_{\text{norm}} = X / 1024$, $Y_{\text{norm}} = Y / 512$).
- **Reasoning:** Mathematical precision eliminates guesswork, enables automated verification scripts, and decouples spatial requirements from specific CSS units or 3D scene units.
- **Advantages:** Unambiguous mathematical precision; pixel-accurate traceability against the reference image; independent of client monitor resolution.
- **Disadvantages:** Requires translation to responsive viewports (1080p, 768p) during layout implementation.
- **Risks:** Misapplying horizontal percentages to vertical dimensions; mitigated by explicit separate $W\%$ and $H\%$ definitions.
- **Affected Components:** `03-COMPOSITION-SPEC.md`, `01-VISUAL-REFERENCE.md`, layout components in Phase 03 and Phase 07.
- **Approval Evidence:** Phase 01 prompt mandate establishing 1024x512 master coordinate system.

