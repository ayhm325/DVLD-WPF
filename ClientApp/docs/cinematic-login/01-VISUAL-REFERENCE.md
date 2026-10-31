# DVLD Cinematic Login — Visual Reference Requirements

> **Document ID:** `01-VISUAL-REFERENCE`  
> **Status:** AUDITED & EMPIRICALLY MEASURED  
> **Authoritative For:** Visual Targets, Element Descriptions, Reference Metadata, and Visual Criteria  
> **Authoritative Coordinate Document:** [`03-COMPOSITION-SPEC.md`](./03-COMPOSITION-SPEC.md)

---

## 1. Verified Reference Metadata

| Property | Verified Measurement Finding | Notes |
|---|---|---|
| **Original Approved Reference Path** | `C:/Users/ayhm3/.gemini/antigravity/brain/d83a43b9-b71f-4032-9bc6-fc2604a045b7/.user_uploaded/media_1793445589710_98905759.jpg` | Verified accessible in workspace context |
| **Bitwise Identical Upload** | `C:/Users/ayhm3/.gemini/antigravity/brain/d83a43b9-b71f-4032-9bc6-fc2604a045b7/.user_uploaded/media_1793448307234_76e52d5e.jpg` | Verified 100% identical byte-for-byte |
| **SHA-256 Checksum** | `c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0` | Verified on both files |
| **Dimensions** | 1024 x 512 pixels | Measured from JPEG SOF0 header |
| **Aspect Ratio** | 2.0000 : 1 | Ultrawide Panoramic Cinematic |
| **Color Space / Bit Depth** | 24-bit sRGB (`Format24bppRgb`) | High dynamic range color distribution |
| **Diagnostic Overlay** | [`evidence/phase-01/diagnostic_bounding_boxes.png`](./evidence/phase-01/diagnostic_bounding_boxes.png) | Annotated review artifact |

---

## 2. Core Visual Elements (Measured Reference Specifications)

### Element 01: Left-Side DVLD Institutional Branding Group (`E01`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `76 px`, Y: `43 px`, W: `224 px`, H: `75 px` ($X$: 7.42%, $Y$: 8.40%, $W$: 21.88%, $H$: 14.65%)
- **Visual Description:** Stacked cyan chevron logo (33x36 px) paired with bold white "DVLD" title (84x31 px), clean subtitle ("Driving & Vehicle Licensing Department"), and active status pill badge ("Licensing operations platform", 140x21 px) with glowing indicator dot.
- **Visual Priority:** Critical (State Institutional Authority)
- **Implementation Layer:** HTML / CSS Presentation Layer.
- **Spatial Clearances:** 76 px left margin; 16 px clearance above headline.

### Element 02: Institutional Messaging & Value Proposition Block (`E02`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `76 px`, Y: `134 px`, W: `260 px`, H: `114 px` ($X$: 7.42%, $Y$: 26.17%, $W$: 25.39%, $H$: 22.27%)
- **Lower Three Pillars:** X: `76 px`, Y: `430 px`, W: `114 px`, H: `45 px` ($X$: 7.42%, $Y$: 83.98%)
- **Visual Description:** Bold two-line headline ("Driving forward." in crisp white, "Managing smarter." in electric cyan), descriptive paragraph explaining modern licensing platform, and lower three-pillar list ("Safer Roads. / Smarter Services. / A Better Tomorrow.") stacked vertically above the lower margin.
- **Visual Priority:** High (Brand Mission & Storytelling)
- **Spatial Clearances:** 44 px clearance gap to the central building entrance portal (X = 336 px to X = 380 px). Overlays in front of the unlit, shadowed western wing of the headquarters facade.

### Element 03: Central Modern Glass-and-Steel Headquarters Complex (`E03`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `195 px`, Y: `200 px`, W: `445 px`, H: `200 px` ($X$: 19.04%, $Y$: 39.06%, $W$: 43.46%, $H$: 39.06%)
- **Visual Description:** Multi-story modern administrative headquarters featuring a grand glass-curtain entrance lobby with warm interior illumination ($\sim 3200\text{K}$ golden glow), vertical structural columns, dark metal roof fascia, and architectural portico.
- **Visual Priority:** Critical (Central 3D Architectural Subject)
- **Implementation Layer:** 3D Scene Environment (Three.js / React Three Fiber GLB Model).
- **Spatial Clearances:** 14 px clearance gap to the right login card border (X = 640 px to X = 654 px).

### Element 04: Bilingual Headquarters Architectural Signage (`E04`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:**
  - English "DVLD" Facade Sign: X: `393 px`, Y: `250 px`, W: `56 px`, H: `35 px`
  - Arabic Canopy Signage: X: `495 px`, Y: `285 px`, W: `130 px`, H: `45 px`
- **Visual Description:** Prominent glowing letters: English "DVLD" centered above the glass entrance; Arabic channel letters `دائرة ترخيص السواقين والمركبات` mounted along the right entrance canopy with cyan-white illumination.
- **Visual Priority:** High (Localization & Architectural Realism)
- **Implementation Layer:** 3D Scene Emissive Mesh / Decal Shader.

### Element 05: Amman Citadel & Temple of Hercules Ruins (`E05`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box (Columns):** X: `520 px`, Y: `85 px`, W: `133 px`, H: `125 px` ($X$: 50.78%, $Y$: 16.60%, $W$: 12.99%, $H$: 24.41%)
- **Visual Description:** Historic Roman Corinthian fluted columns and architrave ruins standing on the elevated rocky limestone hill of Jabal al-Qal'a behind the headquarters, illuminated by warm golden architectural spotlights.
- **Visual Priority:** Critical (Jordanian Cultural Heritage Backdrop)
- **Implementation Layer:** 3D Scene Environment (Three.js / React Three Fiber GLB Model).
- **Spatial Relationship with Jordan Map HUD:** The westernmost columns share horizontal ($X: 520\text{--}570\text{ px}$) and vertical ($Y: 85\text{--}140\text{ px}$) extent with the semi-transparent wireframe Jordan map HUD, which overlays cleanly in front without blocking the columns.

### Element 06: Floating Holographic Jordanian Driving License (`E06`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:**
  - Holographic Card Frame: X: `322 px`, Y: `40 px`, W: `158 px`, H: `158 px` ($X$: 31.45%, $Y$: 7.81%)
  - Conical Emitter Rings: X: `358 px`, Y: `160 px`, W: `88 px`, H: `66 px`
  - Wireframe Jordan Map HUD: X: `490 px`, Y: `35 px`, W: `80 px`, H: `105 px`
- **Visual Description:** Digital transformation motif. Translucent glowing cyan smart driving license card hovering in the sky with 3D perspective pitch and yaw, supported by concentric conical light rings emitting from the building entrance, accompanied by a wireframe Jordan map HUD.
- **Implementation Distinction:**
  - *Reference Concept:* Luminous holographic card hovering in the mid-sky.
  - *Current Codebase Implementation:* 2D SVG/CSS HUD component (`HolographicLicense.tsx`) in DOM presentation layer.
  - *Proposed Future Implementation:* In-scene 3D WebGL projection with custom shader plane (`PROPOSED` for Phase 06/08, subject to stakeholder approval).
- **Visual Priority:** Critical (Non-negotiable Charter Requirement)

### Element 07: Foreground Luxury Sedan (`E07`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `188 px`, Y: `334 px`, W: `188 px`, H: `104 px` ($X$: 18.36%, $Y$: 65.23%, $W$: 18.36%, $H$: 20.31%)
- **Visual Description:** Dark metallic executive luxury sedan angled in the foreground roadway (rear three-quarters view) showing dual-band illuminated red LED taillight bar, rear license plate with Jordanian Arabic script and number ("7-9942"), and glossy metallic reflections.
- **Visual Priority:** High (Grounding Automotive Context)
- **Implementation Layer:** 3D Scene Environment (Three.js / React Three Fiber Model).

### Element 08: Wet Reflective Asphalt Roadway (`E08`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `0 px`, Y: `360 px`, W: `1024 px`, H: `152 px` ($W$: 100.0%, $H$: 29.69%)
- **Visual Description:** Wet dark asphalt pavement with specular mirror puddles reflecting building warm light, red taillight streaks, and cyan curb glow.
- **Visual Priority:** Critical (Cinematic Realism Foundation)
- **Implementation Layer:** 3D Scene Environment PBR Ground Plane.

### Element 09: Illuminated Roadside Curbs & Landscaping (`E09`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `0 px` to `650 px`, Y: `360 px` to `480 px`
- **Visual Description:** Sweeping curving road edge lined with continuous cyan neon LED tubes and desert landscaping shrubs casting cool directional light onto the wet road.
- **Visual Priority:** Medium-High (Urban Environment Framing)
- **Implementation Layer:** 3D Scene Environment Geometry.

### Element 10: Right-Side Glassmorphism Login Panel Frame (`E10`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `654 px`, Y: `52 px`, W: `332 px`, H: `414 px` ($X$: 63.87%, $Y$: 10.16%, $W$: 32.42%, $H$: 80.86%)
- **Visual Description:** Large floating rounded glass container ($r \approx 24\text{ px}$) with dark frosted fill, 20px backdrop blur, luminous cyan rim stroke, and outer cyan halo (X: 630 to 1010 px, Y: 32 to 486 px).
- **Visual Priority:** Critical (Primary Authentication Container)
- **Implementation Layer:** HTML / CSS Presentation Layer (styled with Tailwind and backdrop filters).

### Element 11: Security Crest Emblem & Card Header (`E11`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:**
  - Security Crest Emblem: X: `765 px`, Y: `68 px`, W: `110 px`, H: `87 px`
  - "Welcome back" Title & Subtitle: X: `720 px`, Y: `168 px`, W: `200 px`, H: `54 px`
- **Visual Description:** Concentric glowing cyan ring emblem bearing the Jordanian Hashemite Crown and Security Shield, followed by "Welcome back" (with cyan "back" accent) and access description.
- **Visual Priority:** High (Visual Anchor for Login Card)
- **Implementation Layer:** HTML / CSS Presentation Layer (`SecurityEmblem.tsx`).

### Element 12: Login Credentials Form & Primary Action (`E12`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `676 px`, Y: `236 px`, W: `286 px`, H: `136 px`
- **Visual Description:** Translucent rounded input boxes with user and lock icons, password peek toggle, Remember Me checkbox, Forgot password link, and vibrant gradient "Sign in ->" button (286 x 32 px).
- **Visual Priority:** Critical (Core Authentication Workflow)
- **Implementation Layer:** HTML / CSS Presentation Layer (`Login.tsx`).

### Element 13: Auxiliary SSO Authentication & Security Notice (`E13`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** X: `676 px`, Y: `388 px`, W: `286 px`, H: `76 px`
- **Visual Description:** "OR CONTINUE WITH" divider, row of three auxiliary sign-in pill buttons ("Microsoft", "SSO", "QR Code", each 88x30 px), and green shield badge notice ("Protected by secure token-based authentication.").
- **Visual Priority:** Medium-High (Enterprise Authentication Options)
- **Implementation Layer:** HTML / CSS Presentation Layer (`Login.tsx`).

### Element 14: Cinematic Night Atmosphere & Horizon Radiosity (`E14`)
- **Reference Status:** `VERIFIED & MEASURED`
- **Measured Bounding Box:** Full Canvas ($1024 \times 512\text{ px}$)
- **Visual Description:** Deep celestial midnight blue sky with subtle stars, horizon depth fog, distant city skyline lights on the right horizon (X: 960 to 1024 px), and balanced tone-mapped exposure.
- **Visual Priority:** Critical (Global Atmospheric Unifier)
- **Implementation Layer:** 3D Scene Environment Celestial Lighting & PMREM Map.
