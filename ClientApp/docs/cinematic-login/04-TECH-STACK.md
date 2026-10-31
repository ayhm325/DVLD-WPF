# DVLD Cinematic Login — Technology Stack & Evaluation Policy

> **Document ID:** `04-TECH-STACK`  
> **Status:** AUDITED & VERIFIED  
> **Authoritative For:** Installed Dependencies, Engine Versions, Asset Formats, and Tool Approvals  
> **Audit Date:** 2026-10-31 (Baseline Repository Inspection)

---

## 1. Verified Current Technology Stack

The following dependencies are confirmed installed in `ClientApp/package.json` and resolved in `ClientApp/package-lock.json`:

### 1.1 Core Frontend Framework
| Package | Declared Version | Resolved Lockfile Version | Category |
|---|---|---|---|
| `react` | `^19.3.1` | `19.3.1` | Core UI Library |
| `react-dom` | `^19.3.1` | `19.3.1` | DOM Renderer |
| `typescript` | `~6.0.3` | `6.0.3` | Type System |
| `vite` | `^8.3.4` | `8.3.5` | Build Tool & Dev Server |

### 1.2 3D Graphics & Rendering Pipeline
| Package | Declared Version | Resolved Lockfile Version | Category |
|---|---|---|---|
| `three` | `^0.187.0` | `0.187.0` | Core WebGL/WebGPU 3D Engine |
| `@types/three` | `^0.187.0` | `0.187.0` | TypeScript Definitions |
| `@react-three/fiber` | `^9.8.2` | `9.8.2` | React Three Fiber (R3F) Declarative Wrapper |
| `@react-three/drei` | `^10.7.9` | `10.7.9` | R3F Helper & Abstraction Utilities |

### 1.3 Styling, UI & Motion
| Package | Declared Version | Resolved Lockfile Version | Category |
|---|---|---|---|
| `tailwindcss` | `^4.3.3` | `4.3.3` | Utility-First CSS Framework |
| `@tailwindcss/vite` | `^4.3.3` | `4.3.3` | Vite Tailwind v4 Integration Plugin |
| `motion` | `^14.9.0` | `14.9.0` | Animation Library |
| `clsx` | `^2.1.1` | `2.1.1` | Classname Composition |
| `tailwind-merge` | `^3.7.0` | `3.7.0` | Tailwind Class Merging |
| `class-variance-authority` | `^0.7.1` | `0.7.1` | Component Variant Management |
| `lucide-react` | `^1.65.0` | `1.65.0` | Vector Iconography |

### 1.4 Post-Processing & Special Effects Status
| Technology | Status | Verified Finding |
|---|---|---|
| `@react-three/postprocessing` | **NOT INSTALLED** | Post-processing relies natively on Three.js `<Canvas>` tone-mapping (`ACESFilmicToneMapping`) and custom PBR shaders. |
| `three-stdlib` | Bundled via `@react-three/drei` | Provides loaders and standard Three.js extensions. |

---

## 2. Verified Asset Formats & Existing Tools

| Category | Format / Tool | Current Usage | Status |
|---|---|---|---|
| **3D Models** | `.glb` (Binary GLTF 2.0) | `public/models/headquarters/headquarters.glb`<br>`public/models/citadel/citadel.glb` | Active |
| **Model Exporter** | Three.js `GLTFExporter` in Node.js | `scripts/buildArchitecturalGLBs.js` builds procedural geometry into binary `.glb` without requiring external software. | Verified |
| **Textures** | Procedural Canvas Textures / PNG / WebP | Procedural roughness, normal, and grid maps generated on canvas at runtime. | Verified |
| **Blender** | CLI / Executable | System scan confirmed Blender is **NOT installed** on the host environment (`where blender` returned not found). | Verified |

---

## 3. Technology Evaluation & Approval Policy

In accordance with **Charter Rule 2**, introducing any new package or external tool requires a formal Architectural Decision Record (ADR) evaluated against the following nine criteria:

1. **Problem Definition:** Exactly what visual or functional deficiency cannot be solved with the existing stack?
2. **Existing Stack Limitations:** Why can Three.js `r187`, R3F `v9`, Drei `v10`, or native SVG/CSS not achieve the requirement?
3. **Proposed Technology:** Specific package name, repository, maintainer reputation, and license.
4. **Version Compatibility:** Verification of full compatibility with React 19 (`^19.3.1`) and Three.js 187 (`^0.187.0`).
5. **Bundle & Performance Impact:** Minified + gzipped bundle size increase, GPU draw call overhead, and memory consumption.
6. **Maintenance & Long-Term Cost:** Frequency of upstream updates, dependency footprint, vulnerability history.
7. **Alternative Solutions Considered:** Minimum of two alternative approaches (including pure shader or procedural techniques).
8. **Failure & Risk Analysis:** What happens if the package fails on legacy hardware or mobile browsers?
9. **User Approval Status:** Must be explicitly approved by the user before installation.

---

## 4. Evaluation Candidate Technologies

The following candidates may be formally evaluated in future phases if justified:

| Candidate | Category | Proposed Justification | Current Status |
|---|---|---|---|
| **Blender 4.x** | DCC 3D Authoring | High-fidelity photorealistic vehicle modeling, organic terrain sculpting for Citadel hill, and complex architectural UV mapping. | `PROPOSED — HOST INSTALLATION REQUIRED` |
| **Poly Haven CC0 PBR** | Texture Maps | Photorealistic wet asphalt normal/roughness textures and night sky HDRI radiosity maps. | `PROPOSED — LICENSE & FIT VERIFICATION REQUIRED` |
| `@react-three/postprocessing` | Post-Processing | Screen-space bloom, depth of field, and chromatic aberration. | `DEFERRED — MUST VERIFY REACT 19 COMPATIBILITY` |
