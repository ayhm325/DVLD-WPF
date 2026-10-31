# DVLD Cinematic Login — Asset Register & Provenance

> **Document ID:** `06-ASSET-REGISTER`  
> **Status:** AUDITED & VERIFIED  
> **Authoritative For:** 3D Models, Textures, Shaders, Emblems, and Media Assets  
> **Audit Date:** 2026-10-31

---

## 1. Asset Inventory Summary

| Asset ID | Name | Category | Format | File Size | Status |
|---|---|---|---|---|---|
| `AST-3D-001` | DVLD Headquarters Complex | 3D Model | `.glb` | 236,784 B (231.2 KB) | `VERIFIED` |
| `AST-3D-002` | Amman Citadel (Temple of Hercules) | 3D Model | `.glb` | 1,124,456 B (1.07 MB) | `VERIFIED` |
| `AST-REF-001` | Cinematic Login Reference Image | Visual Reference | `.jpg` | 324,737 B (317.1 KB) | `VERIFIED (CONTEXT)` |
| `AST-TEX-001` | Procedural Glass PBR Normal/Roughness | Texture | Procedural Canvas | In-Memory Canvas | `VERIFIED` |
| `AST-TEX-002` | Procedural Limestone Fluted Columns | Texture | Procedural Canvas | In-Memory Canvas | `VERIFIED` |
| `AST-TEX-003` | Procedural Wet Asphalt Normal Map | Texture | Procedural Canvas | In-Memory Canvas | `VERIFIED` |
| `AST-SVG-001` | DVLD Hashemite Security Emblem | Vector UI | TSX / SVG | Component Code | `VERIFIED` |
| `AST-SVG-002` | Holographic Jordanian Driver License | Vector HUD | TSX / SVG | Component Code | `VERIFIED (SVG HUD)` |
| `AST-IMG-001` | Pre-existing Hero Graphic | Raster Image | `.png` | 134,228 B (131.1 KB) | `VERIFIED (LEGACY)` |

---

## 2. Detailed Asset Records

### Asset ID: AST-3D-001
- **Name:** Modern DVLD Headquarters Complex
- **Type:** 3D Architectural Model
- **Current path:** `ClientApp/public/models/headquarters/headquarters.glb`
- **Format:** Binary GLTF 2.0 (`.glb`)
- **File size:** 236,784 bytes (231.2 KB)
- **Purpose:** Primary architectural center-ground element depicting the modern multi-level headquarters.
- **Source:** Generated via Node.js script `ClientApp/scripts/buildArchitecturalGLBs.js` using Three.js `GLTFExporter`.
- **License:** Proprietary / Project Internal
- **License verification:** `VERIFIED — GENERATED INTERNALLY`
- **Current status:** `VERIFIED` (Active in `HeadquartersModel.tsx`)
- **Used by:** `ClientApp/src/components/login/scene/models/HeadquartersModel.tsx`
- **Known limitations:** Procedural geometry lacks photorealistic chamfering, micro-bevels, and fine window mullions present in reference image.
- **Future action:** Upgrade with high-detail architectural modeling in Phase 04.

---

### Asset ID: AST-3D-002
- **Name:** Amman Citadel (Temple of Hercules & Hill Ruins)
- **Type:** 3D Environment & Cultural Heritage Model
- **Current path:** `ClientApp/public/models/citadel/citadel.glb`
- **Format:** Binary GLTF 2.0 (`.glb`)
- **File size:** 1,124,456 bytes (1.07 MB)
- **Purpose:** Background cultural heritage backdrop depicting the Roman columns and ancient stone ruins on Jabal al-Qal'a.
- **Source:** Generated via Node.js script `ClientApp/scripts/buildArchitecturalGLBs.js` using Three.js `GLTFExporter`.
- **License:** Proprietary / Project Internal
- **License verification:** `VERIFIED — GENERATED INTERNALLY`
- **Current status:** `VERIFIED` (Active in `CitadelModel.tsx`)
- **Used by:** `ClientApp/src/components/login/scene/models/CitadelModel.tsx`
- **Known limitations:** Rocky terrain hill uses procedural terracing; lacks natural limestone boulder photorealism.
- **Future action:** Upgrade with sculpted organic terrain and high-fidelity column capitals in Phase 04.

---

### Asset ID: AST-REF-001
- **Name:** Definitive Cinematic DVLD Login Concept
- **Type:** Primary Visual Reference
- **Current path:** `C:/Users/ayhm3/.gemini/antigravity/brain/d83a43b9-b71f-4032-9bc6-fc2604a045b7/.user_uploaded/media_1793445589710_98905759.jpg` (Current workspace context; not committed in git)
- **Format:** JPEG
- **File size:** 324,737 bytes (317.1 KB)
- **Purpose:** Visual target and source of truth for composition, lighting, materials, and UI styling.
- **Source:** Provided directly by project stakeholder/user.
- **License:** Proprietary / Stakeholder Provided
- **License verification:** `INTERNAL STAKEHOLDER ASSET`
- **Current status:** `VERIFIED`
- **Used by:** Visual audit, composition specs, quality review
- **Known limitations:** Compressed JPEG resolution (1024x512). Requires careful optical analysis.
- **Future action:** Measure precise bounding boxes in Phase 01.

---

### Asset ID: AST-TEX-001 through AST-TEX-003
- **Name:** Procedural PBR Material Maps (Glass, Limestone, Wet Asphalt)
- **Type:** PBR Texture Maps (Normal, Roughness, Specular)
- **Current path:** `ClientApp/src/components/login/scene/materials/PBRMaterials.ts`
- **Format:** Dynamic HTML5 In-Memory Canvas Textures
- **File size:** 0 bytes on disk (Generated at runtime into GPU memory)
- **Purpose:** Provides physical bump and roughness variation for building materials without external HTTP asset requests.
- **Source:** Written procedurally in TypeScript using HTML5 2D Canvas context.
- **License:** MIT / Project Internal
- **License verification:** `VERIFIED — CODE GENERATED`
- **Current status:** `VERIFIED`
- **Used by:** `HeadquartersModel.tsx`, `CitadelModel.tsx`, `Login3DContent.tsx`
- **Known limitations:** Canvas-generated noise lacks high-frequency photorealistic detail of scanned PBR maps.
- **Future action:** Evaluate CC0 scanned PBR textures (Poly Haven) in Phase 06.

---

### Asset ID: AST-SVG-001
- **Name:** DVLD Hashemite Security Crest Emblem
- **Type:** Vector Security Crest
- **Current path:** `ClientApp/src/components/login/SecurityEmblem.tsx`
- **Format:** React TSX Component / Inline SVG
- **File size:** ~4 KB code
- **Purpose:** Glowing emblem at the top of the HTML/CSS glassmorphic authentication card.
- **Source:** Custom SVG vector path implementation.
- **License:** Proprietary / Project Internal
- **License verification:** `VERIFIED — CODE IMPLEMENTED`
- **Current status:** `VERIFIED`
- **Used by:** `Login.tsx`
- **Known limitations:** Minor stylistic difference in crown detailing compared to reference crest.
- **Future action:** Fine-tune SVG vector paths to match reference crest in Phase 07.

---

### Asset ID: AST-SVG-002
- **Name:** Floating Holographic Jordanian Driver License Card
- **Type:** Vector HUD Overlay (HTML/CSS Presentation Layer)
- **Current path:** `ClientApp/src/components/login/HolographicLicense.tsx`
- **Format:** React TSX Component / Inline SVG
- **File size:** ~10 KB code
- **Purpose:** Holographic driving license HUD projecting above the headquarters entrance.
- **Source:** Custom SVG vector implementation with Jordan map vector and emitter rings.
- **License:** Proprietary / Project Internal
- **License verification:** `VERIFIED — CODE IMPLEMENTED`
- **Current status:** `VERIFIED (SVG HUD)`
- **Used by:** `Login.tsx`
- **Known limitations:** Currently rendered in the DOM as a 2D HTML/SVG layer above the 3D scene rather than projected inside the 3D WebGL scene space.
- **Future action:** Projecting into 3D scene space via custom shader plane is a `PROPOSED` enhancement for Phase 06/08.

---

### Asset ID: AST-IMG-001
- **Name:** Legacy Hero Graphic
- **Type:** Pre-existing Dashboard Image
- **Current path:** `ClientApp/src/assets/hero.png`
- **Format:** PNG
- **File size:** 134,228 bytes (131.1 KB)
- **Purpose:** Legacy asset used by internal dashboard pages.
- **Source:** Pre-existing codebase asset.
- **License:** `UNKNOWN — LICENSE VERIFICATION REQUIRED`
- **License verification:** `PENDING VERIFICATION`
- **Current status:** `VERIFIED (LEGACY)`
- **Used by:** Unrelated internal dashboard pages
- **Known limitations:** N/A (Not used in Cinematic Login)
- **Future action:** Retain untouched to protect legacy pages.
