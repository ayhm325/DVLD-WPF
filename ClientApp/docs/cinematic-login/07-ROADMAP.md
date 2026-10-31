# DVLD Cinematic Login — Engineering Roadmap

> **Document ID:** `07-ROADMAP`  
> **Status:** APPROVED SEQUENCE — PHASES SEQUENTIALLY ENFORCED  
> **Authoritative For:** Project Lifecycle, Phase Progression, Prerequisites, and Deliverables

---

## 1. Roadmap Master Summary

| Phase | Title | Primary Objective | Status | Prerequisites |
|---|---|---|---|---|
| **00** | Engineering Documentation Foundation | Establish project charter, technical audit, and governance system | `CHANGES REQUESTED` | Repository Access |
| **00.1** | Documentation Consistency & Governance Correction | Eliminate contradictions, audit ADR approvals, clarify architecture | `APPROVED` | User Phase 01 Prompt |
| **01** | Reference Analysis & Spatial Composition | Measure reference image, define spatial layout, zones, and bounding boxes | `CHANGES REQUESTED` | Phase 00.1 Approval |
| **01.1** | Measurement Integrity Correction | Verify reference image SHA-256, separate 2D/3D depths, correct clearances | `APPROVED` | User Phase 02 Prompt |
| **02** | Design System & Visual Identity | Measure and finalize colors, typography, spacing, and glassmorphic tokens | `CHANGES REQUESTED` | Phase 01.1 Approval |
| **02.1** | Design System Accuracy Correction | Recalculate contrast, classify values, document 1080p scaling, specify 5 components | `CHANGES REQUESTED` | Phase 02 Execution |
| **02.2** | Spatial Consistency & Governance Correction | Align spatial coordinates with Phase 01, reclassify glassmorphism, verify status taxonomy | `CHANGES REQUESTED` | Phase 02.1 Execution |
| **02.3** | Documentation Governance & Coordinate Consistency Correction | Reconcile lifecycle taxonomy with AGENTS.md, enforce continuous half-open coordinates | `TECHNICALLY VERIFIED` | Phase 02.2 Execution |
| **03** | Scene Composition & Spatial Blockout | Calibrate 3D camera, viewports, frustum, and low-poly architectural blockout | `PLANNED` | Phase 02.3 Approval |
| **04** | Architectural Asset Production | Upgrade headquarters and Amman Citadel GLB models with photorealistic detail | `PLANNED` | Phase 03 Approval |
| **05** | Environment & Vehicle Production | Develop foreground luxury sedan, wet road plane, and illuminated neon curbs | `PLANNED` | Phase 04 Approval |
| **06** | Materials, Lighting & Cinematography | Implement PBR shaders, radiosity HDRI, spotlights, and wet reflections | `PLANNED` | Phase 05 Approval |
| **07** | Login Interface Implementation | Refine glassmorphic card, input fields, SSO actions, and left branding | `PLANNED` | Phase 02 & 06 Approval |
| **08** | Scene Integration & Visual Refinement | Unify 3D scene, holographic license HUD, UI card, and global color grading | `PLANNED` | Phase 07 Approval |
| **09** | Responsive & Performance Optimization | Validate 1920x1080 down to mobile viewports, 60 FPS target, and SVG fallback | `PLANNED` | Phase 08 Approval |
| **10** | Final Quality Assurance | Conduct end-to-end visual fidelity check, auth verification, and regression tests | `PLANNED` | Phase 09 Approval |

---

## 2. Detailed Phase Specifications

### Phase 00 — Engineering Documentation Foundation
- **Objective:** Establish the permanent engineering documentation and governance framework.
- **Scope:** Repository inspection, baseline builds/lints, `AGENTS.md`, 13 documentation files, asset register, and decision records.
- **Deliverables:** `ClientApp/AGENTS.md`, `ClientApp/docs/cinematic-login/` complete documentation suite.
- **Lifecycle Status:** `CHANGES REQUESTED`
- **Technical Verification:** `PASSED`
- **User Approval:** `CHANGES REQUESTED` (User requested consistency pass in Phase 00.1).

---

### Phase 00.1 — Documentation Consistency & Governance Correction
- **Objective:** Perform a documentation-only corrective pass on the Phase 00 foundation to eliminate contradictions, distinguish verified facts from proposals, and audit ADR approvals.
- **Scope:** Documentation files in `ClientApp/AGENTS.md` and `ClientApp/docs/cinematic-login/`.
- **Deliverables:** Corrected documentation suite with clarified rendering architecture, standardized status taxonomies, audited ADR approval evidence, and measurable quality gates.
- **Lifecycle Status:** `APPROVED`
- **Technical Verification:** `PASSED`
- **User Approval:** `APPROVED` (Approved via User Phase 01 authorization prompt).

---

### Phase 01 — Reference Analysis & Spatial Composition Specification
- **Objective:** Perform rigorous optical measurement of the 1024x512 reference image to define exact composition geometry and spatial relationships.
- **Scope:** Optical measurement of the 14 core elements, determination of relative X/Y percentages, bounding boxes, depth order, clearances, and responsive planning.
- **Deliverables:** Finalized measurements in `03-COMPOSITION-SPEC.md`, updated `01-VISUAL-REFERENCE.md`, diagnostic overlay in `evidence/phase-01/diagnostic_bounding_boxes.png`.
- **Lifecycle Status:** `CHANGES REQUESTED`
- **Technical Verification:** `PASSED`
- **User Approval:** `CHANGES REQUESTED` (User requested measurement integrity pass in Phase 01.1).

---

### Phase 01.1 — Measurement Integrity Correction
- **Objective:** Authenticate master reference image via SHA-256, remove unsupported physical 3D world depths, correct spatial clearances, separate hologram representation state, and reclassify responsive hypotheses.
- **Scope:** Documentation files `03-COMPOSITION-SPEC.md`, `01-VISUAL-REFERENCE.md`, `08-CURRENT-STATE.md`, `07-ROADMAP.md`, `09-DECISIONS.md`.
- **Deliverables:** Corrected composition specification with verified SHA-256 (`c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0`), pure 2D visual depth order, detailed clearance analysis (description text vs entrance portico and Jordan map HUD vs Citadel columns), and reclassified responsive recommendations.
- **Lifecycle Status:** `APPROVED`
- **Technical Verification:** `PASSED`
- **User Approval:** `APPROVED` (Approved via User Phase 02 authorization prompt — **Last User-Approved Phase**).

---

### Phase 02 — Design System & Visual Identity
- **Objective:** Establish exact color values, typography hierarchy, spacing scale, glassmorphic parameters, lighting language, and visual tokens from reference analysis.
- **Scope:** Hex color codes, opacity levels, font pairings (Arabic & Western), border radius, shadow formulas, component specifications, motion design, and accessibility.
- **Prerequisites:** Explicit User Approval of Phase 01.1.
- **Deliverables:** Fully populated `02-DESIGN-SYSTEM.md` replacing all pending tokens, updated continuity files.
- **Lifecycle Status:** `CHANGES REQUESTED`
- **Technical Verification:** `PASSED`
- **User Approval:** `CHANGES REQUESTED` (User requested accuracy correction in Phase 02.1).

---

### Phase 02.1 — Design System Accuracy Correction
- **Objective:** Perform a focused accuracy, mathematical verification, and completeness pass on the Phase 02 design system.
- **Scope:** Recalculate WCAG relative luminance and contrast ratios mathematically; separate Reference-derived facts from Proposed engineering parameters; document 1080p scaling methodology; author complete specifications for the 5 mandated components (hologram license, Jordan map HUD, footer, 3 mission statements, Arabic signage); verify exact 23-token table count.
- **Prerequisites:** Phase 02 Execution.
- **Deliverables:** Corrected `02-DESIGN-SYSTEM.md`, updated `08-CURRENT-STATE.md`, updated `07-ROADMAP.md`.
- **Lifecycle Status:** `CHANGES REQUESTED`
- **Technical Verification:** `PASSED`
- **User Approval:** `CHANGES REQUESTED` (User requested spatial consistency and governance correction in Phase 02.2).

---

### Phase 02.2 — Spatial Consistency & Governance Correction
- **Objective:** Perform a focused documentation-only correction ensuring that the design system remains consistent with authoritative Phase 01 composition measurements and approved governance rules.
- **Scope:** Audit spatial coordinates in `02-DESIGN-SYSTEM.md` against authoritative `03-COMPOSITION-SPEC.md` (holographic license, Jordan map HUD, solid login card vs outer halo, three lower mission statements, Arabic canopy signage); reclassify glassmorphic parameters as PROPOSED implementation starting points; remove non-standard statuses; enforce accurate Git working tree characterization.
- **Prerequisites:** Phase 02.1 Execution.
- **Deliverables:** Reconciled `02-DESIGN-SYSTEM.md`, updated `08-CURRENT-STATE.md`, updated `07-ROADMAP.md`.
- **Lifecycle Status:** `CHANGES REQUESTED`
- **Technical Verification:** `PASSED`
- **User Approval:** `CHANGES REQUESTED` (User requested documentation governance and coordinate consistency correction in Phase 02.3).

---

### Phase 02.3 — Documentation Governance & Coordinate Consistency Correction
- **Objective:** Strictly align the phase lifecycle taxonomy with canonical `AGENTS.md` and establish the continuous half-open bounding box convention $[x_{\text{min}}, x_{\text{max}}) \times [y_{\text{min}}, y_{\text{max}})$ across all spatial documents.
- **Scope:** Eliminate taxonomy conflicts across roadmap and living state; define continuous half-open interval model in `03-COMPOSITION-SPEC.md` and `02-DESIGN-SYSTEM.md`; reconcile login panel dimensions ($[654, 986) \times [52, 466)$, $W: 332, H: 414$), right margin ($38\text{ px}$), and bottom margin ($46\text{ px}$); accurately document local Git repository working tree state.
- **Prerequisites:** Phase 02.2 Execution.
- **Deliverables:** Reconciled `03-COMPOSITION-SPEC.md`, `02-DESIGN-SYSTEM.md`, `07-ROADMAP.md`, `08-CURRENT-STATE.md`.
- **Lifecycle Status:** `TECHNICALLY VERIFIED`
- **Technical Verification:** `PASSED`
- **User Approval:** `PENDING` (Awaiting explicit user review and approval).

---

### Phase 03 — Scene Composition & Spatial Blockout
- **Objective:** Align the Three.js 3D perspective camera, field of view, and layout planes with the reference composition.
- **Scope:** Update `SceneCamera.tsx`, calibrate camera FOV, target coordinates, and position proxy bounding primitives for headquarters, Citadel, and vehicle.
- **Prerequisites:** Explicit User Approval of Phase 02.3.
- **Deliverables:** Calibrated camera and low-poly spatial blockout in `src/components/login/scene/`.
- **Acceptance Criteria:** Spatial alignment between 3D objects and 2D UI overlay matches reference composition without overlapping left branding.
- **Validation Method:** Viewport screenshot overlay at 1920x1080 and 1366x768.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED` (Phase 03 must not begin until authorized by explicit user prompt).

---

### Phase 04 — Architectural Asset Production
- **Objective:** Upgrade the 3D geometry of the DVLD Headquarters and Amman Citadel into photorealistic architectural assets.
- **Scope:** Rebuild `headquarters.glb` (curtain-wall mullions, glass entrance, lobby depth, illuminated signage) and `citadel.glb` (Roman fluted columns, ruins, rocky terrain hill).
- **Prerequisites:** Explicit User Approval of Phase 03.
- **Deliverables:** Updated GLB assets in `public/models/`, updated model loaders.
- **Acceptance Criteria:** Geometric proportions match reference architecture.
- **Validation Method:** Model inspector verification, visual comparison of silhouettes.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED`

---

### Phase 05 — Environment & Vehicle Production
- **Objective:** Create the foreground luxury sedan, wet asphalt road plane, and illuminated neon curbs.
- **Scope:** 3D model/geometry for luxury sedan (rear three-quarters, taillight bar, license plate), road plane geometry, and curving curb splines with neon tubing.
- **Prerequisites:** Explicit User Approval of Phase 04.
- **Deliverables:** Vehicle 3D asset, road mesh, curb geometry in `Login3DContent.tsx`.
- **Acceptance Criteria:** Vehicle proportions and perspective angle match reference; road covers correct screen percentage.
- **Validation Method:** Visual alignment inspection against reference vehicle position.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED`

---

### Phase 06 — Materials, Lighting & Cinematography
- **Objective:** Implement physically based materials, nighttime radiosity lighting, and wet asphalt reflections.
- **Scope:** PBR glass, dark metal, car paint, and limestone shaders; wet asphalt specular and normal mapping; warm lobby floodlights and cool moonlight tuning.
- **Prerequisites:** Explicit User Approval of Phase 05.
- **Deliverables:** Updated `PBRMaterials.ts`, `SceneLighting.tsx`, and `SceneEnvironment.tsx`.
- **Acceptance Criteria:** Specular reflections on wet road accurately mirror building and taillight lights; balanced exposure with zero blown highlights.
- **Validation Method:** Rendered screenshot inspection and histogram analysis.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED`

---

### Phase 07 — Login Interface Implementation
- **Objective:** Finalize the right-side glassmorphism login card, credentials form, SSO buttons, and left-side branding.
- **Scope:** CSS/Tailwind styling for `login-glass-card`, input fields, Remember Me checkbox, button gradient, SVG security crest, and left branding layout.
- **Prerequisites:** Explicit User Approval of Phase 02 and Phase 06.
- **Deliverables:** Updated `Login.tsx`, `Login.css`, `LoginBranding.tsx`, and `SecurityEmblem.tsx`.
- **Acceptance Criteria:** 1:1 visual match with reference login card; WCAG AA contrast; form submission and keyboard navigation functional.
- **Validation Method:** Pixel-diff review, keyboard focus test, screen-reader audit.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED`

---

### Phase 08 — Scene Integration & Visual Refinement
- **Objective:** Harmonize all 3D scene elements, holographic driver license HUD, and 2D UI into one unified cinematic composition.
- **Scope:** Depth layering, integration of holographic license with emitter rings, global color grading adjustments, and seam elimination.
- **Prerequisites:** Explicit User Approval of Phase 07.
- **Deliverables:** Integrated cinematic login experience.
- **Acceptance Criteria:** All 14 visual elements present, aligned, and cohesive; zero unintended overlaps.
- **Validation Method:** Multi-resolution visual comparison against reference image.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED`

---

### Phase 09 — Responsive & Performance Optimization
- **Objective:** Optimize performance for consistent 60 FPS rendering and ensure responsive behavior.
- **Scope:** Viewport testing (1920x1080, 1366x768, 1280x720, 1024x768, mobile), draw call optimization, WebGL fallback verification.
- **Prerequisites:** Explicit User Approval of Phase 08.
- **Deliverables:** Responsive CSS adjustments, performance tuning in R3F scene.
- **Acceptance Criteria:** Stable 60 FPS on standard desktop GPUs; clean SVG fallback on simulated WebGL failure.
- **Validation Method:** FPS profiling, Lighthouse audit, fallback simulation test.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED`

---

### Phase 10 — Final Quality Assurance
- **Objective:** Perform comprehensive visual fidelity, functional regression, and production readiness sign-off.
- **Scope:** Complete audit against Project Charter, 14 visual elements, authentication flows, error handling, and production build checks.
- **Prerequisites:** Explicit User Approval of Phase 09.
- **Deliverables:** Final QA report, updated documentation, production build verification.
- **Acceptance Criteria:** 100% compliance with Quality Gates defined in `10-QUALITY-GATES.md`.
- **Validation Method:** End-to-end login flow test, production bundle size audit, final user visual sign-off.
- **Lifecycle Status:** `PLANNED`
- **Technical Verification:** `NOT STARTED`
- **User Approval:** `UNAUTHORIZED`
