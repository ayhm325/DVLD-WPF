# DVLD Cinematic Login — Acceptance Criteria & Quality Gates

> **Document ID:** `10-QUALITY-GATES`  
> **Status:** ENFORCED & MANDATORY  
> **Authoritative For:** Phase Acceptance, Review Checklists, and Production Readiness Standards

---

## 1. Quality Gate Framework Overview

Every development phase must pass a multi-dimensional quality assessment before being submitted for user review.

> [!IMPORTANT]
> A phase **cannot** pass merely because the application builds successfully (`npm run build` exits 0). Technical compilation is a necessary prerequisite, but visual fidelity, functional integrity, and explicit stakeholder approval are equally non-negotiable.

---

## 2. Gate 1: Visual Quality Standards

Visual quality is assessed against the definitive reference image (`media_1793445589710_98905759.jpg`):

- [ ] **Reference Fidelity:** Scene composition, proportions, and visual hierarchy reproduce the reference layout.
- [ ] **Completeness:** All 14 mandatory visual elements are present (or accounted for according to phase roadmap).
- [ ] **Spatial Boundaries & Clearances:** No visual collisions or overlaps occur between architectural elements, 3D objects, and left-side typography.
- [ ] **Arabic Orthography & Typography:** Arabic building signage (`دائرة ترخيص السواقين والمركبات`) and UI labels are orthographically correct, properly connected (non-isolated glyphs), and legible.
- [ ] **Material Realism:** PBR shaders exhibit physically plausible roughness, metalness, and specular highlights without looking like flat vector art or untextured clay.
- [ ] **Nighttime Cinematography:** Directional amber lighting on the Citadel and building lobby contrasts cleanly with deep celestial navy skies without clipping or blowing out highlights.
- [ ] **Reflections:** Wet asphalt road shows coherent, distorted specular reflections of vehicle brake lights and illuminated architectural facades.

---

## 3. Gate 2: Technical & Performance Quality Standards

### 3.1 General Technical Adherence
- [ ] **Zero Unauthorized Dependencies:** No packages added to `package.json` without an approved ADR in `09-DECISIONS.md`.
- [ ] **TypeScript Type Safety:** Clean compilation with `tsc -b` with zero `any` evasions, zero type suppressions, and zero compiler warnings.
- [ ] **Production Build Integrity:** `npm.cmd --prefix ClientApp run build` passes with exit code 0.
- [ ] **Linter Adherence:** `npm.cmd --prefix ClientApp run lint` executes with zero errors and zero warnings.
- [ ] **Runtime Console Cleanliness:** Zero uncaught exceptions, WebGL warnings, or missing asset 404 errors in browser developer tools.

### 3.2 Measurable Performance Specifications
The following performance targets define test environments, measurement methods, and required evidence:

| Metric | Measurement Method | Test Environment | Measurement Timing | Acceptance Threshold | Evidence Required |
|---|---|---|---|---|---|
| **Render Framerate** | Chrome DevTools Performance panel / `stats.js` FPS monitor | Chromium on standard desktop GPU (1080p, 60Hz display) | Steady-state scene rendering after initial load | $\ge$ 55 FPS sustained (Target: 60 FPS) | 10-second FPS profile recording |
| **Initial 3D Paint** | Performance Timeline (`mark` / `measure`) | Fast 3G / Local network simulation | Page mount to first Three.js frame render | $\le$ 2.5s initial mount | DevTools Performance summary |
| **3D Asset Footprint** | Direct file-size measurement of exported `.glb` files | Local disk / build output | Post-build / export in Phase 04 & 05 | `PROPOSED THRESHOLD — BASELINE MEASUREMENT REQUIRED`<br>(Target: $< 3.5$ MB total 3D assets, subject to visual fidelity baseline) | `ls -lh` or build artifact size report |
| **Polygon / Triangle Count** | Three.js `renderer.info.render.triangles` | Runtime WebGL inspection | Loaded scene with all models active | `PROPOSED THRESHOLD — BASELINE MEASUREMENT REQUIRED`<br>(Target: $< 50,000$ triangles total, subject to architectural detail needs) | Console log of `gl.info` |
| **GPU Memory Usage** | Chrome Task Manager (GPU Memory column) | Chromium desktop, 1080p viewport | Active rendering session | $\le$ 250 MB dedicated GPU memory | Chrome Task Manager snapshot |
| **Motion Accessibility** | Emulate CSS `prefers-reduced-motion: reduce` in DevTools | Chromium browser emulation | Runtime animation execution | Zero camera sway, zero continuous pulsing; static scene render | Visual recording / test assertion |

> [!NOTE]
> Asset size and triangle count limits are marked `PROPOSED THRESHOLD — BASELINE MEASUREMENT REQUIRED`. They must not be used to arbitrarily degrade visual quality before establishing an empirical baseline in Phase 04 and 05.

---

## 4. Gate 3: Functional & Security Standards

- [ ] **Authentication Flow Unbroken:** Submitting valid credentials correctly calls `authService.login()` and updates session context.
- [ ] **Input Form Resilience:** Username and password inputs handle focus, peek toggle, auto-fill, and error states without degradation.
- [ ] **"Remember Me" Preservation:** Correctly reads and writes `dvld_remembered_username` in `localStorage`.
- [ ] **SSO Action Readiness:** Auxiliary sign-in buttons trigger designated events or appropriate modal dialogs.
- [ ] **Graceful WebGL Fallback:** Simulating a WebGL failure (e.g. disabling WebGL in browser flags or triggering context loss via `gl.getExtension('WEBGL_lose_context').loseContext()`) causes `<Login3DScene />` to mount `<CinematicEnvironment />` smoothly without throwing an unhandled runtime error.
- [ ] **Protected Subsystems Unmodified:** Zero changes to backend code, database schemas, WPF client files, or internal dashboard pages.

---

## 5. Gate 4: Phase Completion & Documentation Standards

- [ ] **Scope Confinement:** Only deliverables explicitly authorized in the phase prompt were implemented.
- [ ] **Zero Unapproved Git Operations:** No commits, pushes, branch creations, or destructive working tree resets.
- [ ] **Working Tree Safety:** Pre-existing uncommitted modifications were preserved.
- [ ] **Documentation Synchronization:**
  - `08-CURRENT-STATE.md` updated with exact phase delta, verified status, and next authorized actions.
  - `07-ROADMAP.md` updated with phase lifecycle status.
  - `06-ASSET-REGISTER.md` updated if assets were created or modified.
  - `09-DECISIONS.md` updated if architectural or design decisions were made.
- [ ] **Standard Final Report:** Phase report delivered using the mandatory 8-section format (A through H).
- [ ] **Explicit Stop Condition:** Agent ceases all tool calls and awaits written user approval before proceeding to the next phase.
