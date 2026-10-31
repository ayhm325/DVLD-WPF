# DVLD Cinematic Login — Project Charter

> **Document ID:** `00-PROJECT-CHARTER`  
> **Status:** APPROVED & MANDATORY  
> **Authoritative For:** Scope, Rules, Visual Authority, and Governance Boundaries

---

## 1. Project Objective

The objective of the DVLD Cinematic Login project is to elevate the web authentication experience for the Jordanian Driving & Vehicle Licensing Department (DVLD) into a photorealistic, cinematic nighttime 3D architectural visualization.

The target system combines:
- A high-fidelity physically-based 3D environment depicting the DVLD Modern Headquarters and the historic Amman Citadel (Temple of Hercules) rendered via Three.js / React Three Fiber.
- Atmospheric nighttime cinematography featuring wet asphalt reflections, directional architectural uplighting, illuminated curbs, and deep blue-cyan ambient depth.
- An interactive holographic Jordanian driving license overlay (currently implemented as an SVG/CSS layer; in-scene 3D projection is a proposal for evaluation in Phase 06/08).
- A foreground luxury sedan positioned on the wet roadway.
- A glassmorphic authentication card implemented in HTML/CSS with high legibility, accessible form controls, and luminous cyan accents, floating as a presentation layer over the 3D scene.
- A client-side WebGL capability detection and React Error Boundary that automatically mounts an SVG/CSS fallback environment if WebGL is unavailable or encounters an unhandled runtime error.

---

## 2. Core Mandatory Rules

### Rule 1 — The Reference Image Is the Source of Truth
The user-supplied cinematic DVLD login reference image is the primary visual authority for all architectural, lighting, spatial, and styling decisions.
- **Accuracy:** The final scene must reproduce the reference's composition, visual hierarchy, proportions, spatial relationships, atmosphere, and essential visual elements as faithfully as technically possible.
- **No Unapproved Redesigns:** The reference design must not be replaced with a generic dashboard, unrelated 3D concept, or arbitrary vector layout.
- **Preserve Key Elements:** Distinctive visual components (especially the Amman Citadel, illuminated DVLD glass facade, foreground sedan, and floating holographic driving license) must not be omitted or simplified without explicit user consent.
- **True Layered Scene:** The reference image must **never** be inserted as a flattened CSS background or static `<img>` overlay. The scene must be rendered dynamically with controllable 3D and UI layers.
- **Reference Asset Tracking:** The reference image in the current workspace context is located at `C:/Users/ayhm3/.gemini/antigravity/brain/d83a43b9-b71f-4032-9bc6-fc2604a045b7/.user_uploaded/media_1793445589710_98905759.jpg` (uploaded by user; not committed to git repository).

### Rule 2 — Technology Changes Require Approval
- **Inspect Before Proposing:** Inspect installed dependencies (`package.json`) before suggesting any library or tool.
- **Approval Required:** Do not install, update, remove, or substitute packages without documenting an ADR in `09-DECISIONS.md` and obtaining explicit user approval.
- **Version Discipline:** Never assume a version number. Verify it from `package.json` or `package-lock.json`.
- **Prefer Existing Stack:** Maximize the capabilities of the already installed Three.js (`^0.187.0`), React Three Fiber (`^9.8.2`), Drei (`^10.7.9`), React 19, and Tailwind CSS v4 before proposing external packages.

### Rule 3 — Strict Phase Approval Lifecycle
The project progresses strictly through sequentially authorized phases following this lifecycle:

$$\text{Planned} \longrightarrow \text{In Progress} \longrightarrow \text{Technically Verified} \longrightarrow \text{Visual Review} \longrightarrow \begin{matrix} \text{Approved} \\ \updownarrow \\ \text{Changes Requested} \end{matrix}$$

- **One Phase at a Time:** Only the single active phase authorized by the user may be executed.
- **Stop on Completion:** When a phase's deliverables are implemented and verified, the agent must **STOP** immediately and present the completion report.
- **Explicit Approval Only:** Advancement to the next phase requires explicit user authorization. A successful build, absence of errors, or silence does not constitute approval.

### Rule 4 — Application Functionality Must Remain Protected
The cinematic login experience is a presentation enhancement. It must remain strictly isolated from backend and business services. Unless explicitly commanded in a dedicated authentication phase, the following are strictly protected:
- Authentication logic (`src/services/authService.ts`, `src/context/useAuth.ts`)
- Login form validation, input contracts, and submission handlers
- Token storage and session management
- API services (`DVLD_API`), business layers (`DVLD_Business`), data access (`DVLD_DataAccess`), and database schema
- Desktop WPF application code (`DVLD_Presentation`)
- Authenticated application routes and pages

### Rule 5 — No Unapproved Repository Operations
- **No Commits / Pushes:** Never execute `git commit` or `git push` unless explicitly asked.
- **No Branching:** Do not create or switch branches.
- **Preserve Working Tree:** Never clean, reset, discard, or overwrite uncommitted modifications in the working tree.

---

## 3. Mandatory Visual Elements Checklist

The following 14 core elements from the reference image are mandatory:

1. **DVLD Left-Side Branding & Emblem:** Distinctive stacked chevron logo, title, and "Licensing operations platform" badge.
2. **Institutional Messaging:** "Driving forward. Managing smarter." typography hierarchy and supporting mission statement.
3. **Central DVLD Headquarters:** Modern multi-story glass-and-steel architectural complex with interior warm illumination and structural curtain walls.
4. **Bilingual Headquarter Signage:** English "DVLD" illuminated signage above the main entrance and Arabic illuminated signage (`دائرة ترخيص السواقين والمركبات`) across the entrance canopy.
5. **Amman Citadel & Temple of Hercules:** Elevated rocky hill behind the headquarters featuring the ancient Roman fluted columns and ruins bathed in golden architectural uplighting.
6. **Floating Holographic Jordanian Driving License:** Highly detailed cyan/blue holographic driver license card projecting into the night sky with concentric conical emitter rings. (Currently rendered via SVG HUD; in-scene 3D projection is a proposed enhancement).
7. **Foreground Luxury Sedan:** Dark metallic luxury sedan on the wet roadway showing illuminated tail lamps and realistic rear styling.
8. **Wet Reflective Asphalt Roadway:** PBR road plane with high specularity, puddles, lane markings, and reflections of building lights and vehicle tail lamps.
9. **Illuminated Roadside Curbs & Landscaping:** Curving roadway perimeter featuring continuous LED neon curb lighting strips and desert shrubs.
10. **Right-Side Glassmorphism Login Panel:** Floating HTML/CSS frosted glass container with high blur, luminous cyan outline, and internal padding.
11. **Security Crest Emblem & Header:** Glowing Jordanian-inspired DVLD security shield emblem, "Welcome back" title, and subtitle.
12. **Login Credentials Form:** Floating input fields for username and password with iconography, Remember Me toggle, and cyan-blue gradient "Sign in ->" button.
13. **Auxiliary SSO Authentication:** Alternative authentication buttons ("Microsoft", "SSO", "QR Code") and token-based protection footer notice.
14. **Cinematic Night Atmosphere & Horizon:** Deep navy/cyan celestial night sky with subtle stars, distant city skyline lights, and unified color grading.

---

## 4. Explicitly Prohibited Actions

The following actions are strictly prohibited across all phases unless overridden by explicit written user instructions:
- Flattening the scene into a static raster image.
- Removing or replacing the holographic driving license.
- Breaking responsive layout support on standard enterprise viewports (1920x1080, 1366x768, 1280x720, 1024x768).
- Disabling the WebGL error boundary and SVG fallback mechanism.
- Installing heavy third-party scene editors or unauthorized post-processing packages.
- Bypassing sandbox security controls or modifying Git history.
