# DVLD Cinematic Login — Spatial Composition & Layout Specification

> **Document ID:** `03-COMPOSITION-SPEC`  
> **Status:** AUDITED & EMPIRICALLY MEASURED — AUTHORITATIVE SPECIFICATION  
> **Authoritative For:** Spatial Layout, Element Bounding Boxes, Depth Layers, Clearances, and Viewport Planning  
> **Master Visual Reference:** `media_1793445589710_98905759.jpg` (Original Approved Reference Image)  
> **Reference Hash Verification:** SHA-256 `c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0` (Identical bit-for-bit to `media_1793448307234_76e52d5e.jpg`)  
> **Measurement Baseline:** 1024 x 512 pixels (2.0000 : 1 aspect ratio)

---

## 1. Reference Canvas Geometry & Master Coordinate System

### 1.1 Master Canvas & Coordinate System
| Metric | Measured Reference Finding | Conversion / Specification |
|---|---|---|
| **Canvas Dimensions** | 1024 x 512 pixels | Measured from JPEG header SOF0 marker |
| **Aspect Ratio** | 2.0000 : 1 | Ultrawide panoramic cinematic ratio |
| **Master Origin (0,0)** | Top-Left Corner | Canonical Continuous Range: $X \in [0, 1024)\text{ px}$ (Left to Right), $Y \in [0, 512)\text{ px}$ (Top to Bottom) |
| **Continuous Coordinate Model** | Half-Open Intervals | $[x_{\text{min}}, x_{\text{max}}) \times [y_{\text{min}}, y_{\text{max}})$ |
| **Normalized Coordinates** | $X_{\text{norm}} \in [0.0, 1.0)$, $Y_{\text{norm}} \in [0.0, 1.0)$ | $X_{\text{norm}} = X / 1024$, $Y_{\text{norm}} = Y / 512$ |
| **Percentage Coordinates** | $X\% \in [0.0\%, 100.0\%)$, $Y\% \in [0.0\%, 100.0\%)$ | $X\% = (X / 1024) \times 100$, $Y\% = (Y / 512) \times 100$ |
| **Width / Height Scale** | $W\% = (W / 1024) \times 100$, $H\% = (H / 512) \times 100$ | Horizontal and vertical percentages are independent |

### 1.2 Canonical Continuous Half-Open Bounding Box Convention
To eliminate mathematical ambiguities between discrete pixel indices and geometric boundaries, all spatial coordinates in this project strictly follow the **continuous half-open interval convention**:

$$\text{Bounding Box} = [x_{\text{min}}, x_{\text{max}}) \times [y_{\text{min}}, y_{\text{max}})$$

- **Canvas Geometric Extent:** $[0, 1024) \times [0, 512)$
- **Width:** $W = x_{\text{max}} - x_{\text{min}}$
- **Height:** $H = y_{\text{max}} - y_{\text{min}}$
- **Right Geometric Boundary:** $x_{\text{max}} = x_{\text{min}} + W$
- **Bottom Geometric Boundary:** $y_{\text{max}} = y_{\text{min}} + H$
- **Left Margin:** $x_{\text{min}} - 0 = x_{\text{min}}$
- **Right Margin to Canvas Edge:** $1024 - x_{\text{max}} = 1024 - (x_{\text{min}} + W)$
- **Top Margin:** $y_{\text{min}} - 0 = y_{\text{min}}$
- **Bottom Margin to Canvas Edge:** $512 - y_{\text{max}} = 512 - (y_{\text{min}} + H)$
- **Pixel-Center Sampling Distinction:** An integer pixel at column $i \in \{0, \dots, 1023\}$ and row $j \in \{0, \dots, 511\}$ occupies the continuous area $[i, i+1) \times [j, j+1)$ with its optical center at $(i + 0.5, j + 0.5)$. When pixel colors are sampled, coordinates represent these optical centers; when boundaries are defined, coordinates represent continuous geometric edges.

> [!NOTE]
> **Resolution of Historical Discrepancy:** Under discrete inclusive pixel indexing, an element of width 332 starting at pixel index 654 has its final pixel at index $654 + 332 - 1 = 985$. When earlier documentation mixed discrete index 985 with continuous canvas boundary 1024, it reported an erroneous margin of $1024 - 985 = 39\text{ px}$. Under the continuous half-open convention $[654, 986) \times [52, 466)$, the geometric right boundary is exactly $x_{\text{max}} = 986\text{ px}$, and the right margin to canvas boundary is exactly $1024 - 986 = \mathbf{38\text{ px}}$. Similarly, the bottom boundary is $y_{\text{max}} = 52 + 414 = 466\text{ px}$, and the bottom margin is $512 - 466 = \mathbf{46\text{ px}}$.

---

## 2. Master Visual Zones

The reference composition partitions the 1024 x 512 canvas into six interconnected visual zones:

```text
+----------------------------------------------------------------------------------------------------+
| Zone F: Celestial Night Sky & Atmosphere ($Y \in [0, 220)\text{ px}$)                                       |
|                                                                                                    |
| Zone A: Left Branding       Zone D: Holographic License    Zone B: Citadel Ruins    Zone E:        |
| - Chevron Logo              - Floating Card (322,40)       - Roman Columns (520,85) | Glassmorphic |
| - DVLD Title (126,45)       - Emitter Cone (358,160)       - Rocky Hill Ridge       | Login Card   |
| - Slogan (76,134)           - Jordan Map Wireframe (490,35)                         | Frame        |
| - Description (76,210)                                                              | [654, 986)   |
|                             Zone B: Central Headquarters Complex                    | x [52, 466)  |
|                             - Glass Facade, Lobby & Entrance Portico (195,200)      |              |
|                             - English Sign "DVLD" (393,250)                         | Width: 332px |
| Zone C: Vehicle & Road      - Arabic Signage (495,285)                              | Height:414px |
| - Wet Reflective Roadway Plane (Full Width, $Y \in [360, 512)\text{ px}$)                           |              |
| - Curving Neon Curbs (Lower-Left to Center-Right)                                   |              |
+----------------------------------------------------------------------------------------------------+
```

### Zone A: Left Branding & Institutional Identity (Bounds: [76, 336) × [43, 475) px)
- Houses institutional identity, mission copy, and core value statements.
- Occupies the left-hand column (~25.4% canvas width); text overlays over the darkened, shadowed western facade of the headquarters complex without visual collision.

### Zone B: Central Architectural Complex (Bounds: [195, 653) × [85, 400) px)
- Centerpiece modern multi-story glass-and-steel headquarters building, illuminated lobbies, entrance canopy, and bilingual signage.
- Historic Amman Citadel and Temple of Hercules columns rise directly behind the building on the elevated rocky hill ridge.

### Zone C: Foreground Roadway & Automotive (Bounds: [0, 1024) × [334, 512) px)
- Ground plane covered by wet reflective asphalt reflecting ambient lighting, vehicle tail lamps, and neon curbs.
- Foreground luxury sports sedan positioned at an angle on the road (rear three-quarters).

### Zone D: Holographic License Digital Transformation Area (Bounds: [322, 570) × [35, 226) px)
- Luminous cyan floating driver's license card projecting into the mid-sky above the headquarters portico.
- Anchored by concentric conical emitter rings originating from the building entrance, accompanied by a semi-transparent wireframe Jordan map HUD to the right.

### Zone E: Right Interactive Authentication Panel (Bounds: [654, 986) × [52, 466) px)
- Prominent floating frosted glass container (Bounds: `[654, 986) × [52, 466) px`, Width: 332 px, Height: 414 px, 32.42% x 80.86% canvas).
- Houses Hashemite security crest, welcome header, credentials inputs, remember me, sign in button, SSO buttons, and legal footer.

### Zone F: Global Atmospheric Environment (Bounds: [0, 1024) × [0, 360) px)
- Nocturnal starry sky, atmospheric depth haze, distant city horizon lights (Bounds: `[960, 1024) × [280, 380) px`), and global radiosity.

---

## 3. Element Measurement Register (All 14 Mandatory Elements)

### Element 01: Left-Side DVLD Institutional Branding Group
- **Element ID:** `E01-BRAND-HEADER`
- **Element Name:** DVLD Institutional Logo, Title, Subtitle, and Operations Pill Badge
- **Reference Evidence:** High-contrast white and cyan vector typography and stacked chevron icon.
- **Visual Zone:** Zone A (Left Branding)
- **Bounding Box (Overall Group):**
  - Canonical Bounds: `[76, 300) × [43, 118) px` ($W: 224\text{ px}, H: 75\text{ px}$)
  - *Sub-elements:*
    - Stacked Chevron Logo: Bounds `[76, 109) × [43, 79) px` ($W: 33\text{ px}, H: 36\text{ px}$)
    - "DVLD" Acronym Title: Bounds `[126, 210) × [45, 76) px` ($W: 84\text{ px}, H: 31\text{ px}$)
    - Subtitle ("Driving & Vehicle Licensing Department"): Bounds `[126, 300) × [75, 88) px` ($W: 174\text{ px}, H: 13\text{ px}$)
    - Active Status Badge ("Licensing operations platform"): Bounds `[76, 216) × [97, 118) px` ($W: 140\text{ px}, H: 21\text{ px}$)
- **Normalized Coordinates:**
  - $X$: `0.0742` (7.42%), $Y$: `0.0840` (8.40%), $W$: `0.2188` (21.88%), $H$: `0.1465` (14.65%)
- **Center Point:** $X_{\text{center}}$: `188 px` (18.36%), $Y_{\text{center}}$: `80.5 px` (15.72%)
- **Alignment:** Left-aligned to X = 76 px; top-aligned with upper margins.
- **Spatial Relationships:**
  - Nearest Elements: Main Slogan (`E02`) below (gap: 16 px).
  - Overlapping Elements: None.
  - Elements Behind: Celestial starry night sky (`E14`).
  - Elements In Front: None (UI layer).
- **Visual Priority:** Critical (State Identity Anchor)
- **Depth Layer:** Layer 6A (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** Pixel-level luminance thresholding via System.Drawing.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 1 \text{ px}$
- **Notes:** Clear margin of 76 px from left edge.

---

### Element 02: Institutional Messaging & Value Proposition Block
- **Element ID:** `E02-BRAND-COPY`
- **Element Name:** "Driving forward. Managing smarter." Headline, Mission Paragraph & Three Pillars
- **Reference Evidence:** Crisp sans-serif western typography.
- **Visual Zone:** Zone A (Left Branding)
- **Bounding Box (Headline & Paragraph):**
  - Canonical Bounds: `[76, 336) × [134, 248) px` ($W: 260\text{ px}, H: 114\text{ px}$)
  - *Sub-elements:*
    - Headline ("Driving forward. / Managing smarter."): Bounds `[76, 306) × [134, 198) px` ($W: 230\text{ px}, H: 64\text{ px}$)
    - Explanatory Description: Bounds `[76, 336) × [210, 248) px` ($W: 260\text{ px}, H: 38\text{ px}$)
    - Lower Three Pillars ("Safer Roads. / Smarter Services. / A Better Tomorrow."): Bounds `[76, 190) × [430, 475) px` ($W: 114\text{ px}, H: 45\text{ px}$)
- **Normalized Coordinates:**
  - Headline/Desc: $X$: `0.0742` (7.42%), $Y$: `0.2617` (26.17%), $W$: `0.2539` (25.39%), $H$: `0.2227` (22.27%)
  - Three Pillars: $X$: `0.0742` (7.42%), $Y$: `0.8398` (83.98%), $W$: `0.1113` (11.13%), $H$: `0.0879` (8.79%)
- **Center Point:** Headline/Desc: (206 px, 191 px); Pillars: (133 px, 452.5 px)
- **Alignment:** Strictly flush-left to X = 76 px.
- **Spatial Relationships:**
  - Nearest Elements: Central headquarters entrance portico (`E03`) to the right (horizontal clearance: 44 px).
  - Background Context: The text block overlays the shadowed, unlit western wing of the headquarters building facade (Bounds: `[195, 335) px`), which provides high-contrast dark backing.
- **Visual Priority:** High (Brand Mission)
- **Depth Layer:** Layer 6A (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** Optical contrast edge scanning.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 2 \text{ px}$
- **Notes:** The gap between the rightmost extent of the description ($X = 336\text{ px}$) and the bright central entrance portal ($X = 380\text{ px}$) is 44 px.

---

### Element 03: Central Modern Glass-and-Steel Headquarters Complex
- **Element ID:** `E03-HQ-ARCHITECTURE`
- **Element Name:** Modern DVLD Architectural Headquarters Complex
- **Reference Evidence:** Multi-level glass curtain-wall structure with structural pillars, illuminated interior offices, and entrance portico.
- **Visual Zone:** Zone B (Central Architecture)
- **Bounding Box (Total Building Silhouette):**
  - Canonical Bounds: `[195, 640) × [200, 400) px` ($W: 445\text{ px}, H: 200\text{ px}$)
  - *Key Structural Sub-regions:*
    - Left Structural Wing (Recessed/Shadowed): Bounds `[195, 335) × [240, 380) px` ($W: 140\text{ px}, H: 140\text{ px}$)
    - Central Entrance Portico / Lobby (Brightly Lit): Bounds `[340, 480) × [215, 390) px` ($W: 140\text{ px}, H: 175\text{ px}$)
    - Right Canopy & Extended Wing: Bounds `[480, 640) × [260, 385) px` ($W: 160\text{ px}, H: 125\text{ px}$)
- **Normalized Coordinates:**
  - $X$: `0.1904` (19.04%), $Y$: `0.3906` (39.06%), $W$: `0.4346` (43.46%), $H$: `0.3906` (39.06%)
- **Center Point:** $X_{\text{center}}$: `417.5 px` (40.77%), $Y_{\text{center}}$: `300 px` (58.59%)
- **Alignment:** Anchored to middle-ground ground line ($y \in [385, 390)\text{ px}$).
- **Spatial Relationships:**
  - Nearest Elements: Holographic license (`E06`) above; Luxury sedan (`E07`) in foreground; Citadel (`E05`) behind; Login card (`E10`) to the right.
  - Overlapping Elements: Foreground sedan (`E07`) occludes left wing base; Left branding (`E02`) overlays front of shadowed left wing.
  - Elements Behind: Amman Citadel rocky hill (`E05`).
- **Visual Priority:** Critical (Primary Architectural Focal Point)
- **Depth Layer:** Layer 3 (3D Middle-Ground Environment)
- **Measurement Method:** Building silhouette detection and interior warm illumination bounding.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 4 \text{ px}$ (outer roofline edge blending with hill shadows).
- **Notes:** Central architectural centerpiece occupying over 43% of total image width.

---

### Element 04: Bilingual Headquarters Architectural Signage
- **Element ID:** `E04-HQ-SIGNAGE`
- **Element Name:** Illuminated English "DVLD" & Arabic Entrance Canopy Channel Letters
- **Reference Evidence:** Emissive cyan-white illuminated signage mounted to building facade.
- **Visual Zone:** Zone B (Central Architecture)
- **Bounding Box:**
  - English "DVLD" Facade Sign (`E04a`): Canonical Bounds `[393, 449) × [250, 285) px` ($W: 56\text{ px}, H: 35\text{ px}$)
  - Arabic Canopy Sign (`E04b`, `دائرة ترخيص السواقين والمركبات`): Canonical Bounds `[495, 625) × [285, 330) px` ($W: 130\text{ px}, H: 45\text{ px}$)
- **Normalized Coordinates:**
  - English Sign: $X$: `0.3838` (38.38%), $Y$: `0.4883` (48.83%), $W$: `0.0547` (5.47%), $H$: `0.0684` (6.84%)
  - Arabic Sign: $X$: `0.4834` (48.34%), $Y$: `0.5566` (55.66%), $W$: `0.1270` (12.70%), $H$: `0.0879` (8.79%)
- **Center Point:** English: (421 px, 267.5 px); Arabic: (560 px, 307.5 px)
- **Alignment:** English sign centered above main glass portal; Arabic sign along right canopy fascia.
- **Spatial Relationships:**
  - Nearest Elements: HQ entrance lobby (`E03`); Emitter rings (`E06`) rise directly above English sign.
- **Visual Priority:** High (Institutional Authenticity & Localization)
- **Depth Layer:** Layer 3 (3D Scene Environment Decal / Emissive Shader)
- **Measurement Method:** Emissive color thresholding.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 2 \text{ px}$
- **Notes:** Signage is physically anchored to the building model. Canopy structure ends at $x_{\text{max}} \approx 640\text{ px}$, maintaining a strict $14\text{ px}$ clearance to the solid login card border ($x_{\text{min}} = 654\text{ px}$, clearance interval $[640, 654)\text{ px}$).

---

### Element 05: Amman Citadel & Temple of Hercules Ruins
- **Element ID:** `E05-CITADEL-RUINS`
- **Element Name:** Ancient Roman Fluted Columns, Architrave & Rocky Jabal al-Qal'a Hill
- **Reference Evidence:** Warm amber-lit Roman fluted columns and limestone archaeological ruins on the natural hillside.
- **Visual Zone:** Zone B / Zone F (Background Heritage Backdrop)
- **Bounding Box (Columns & Upper Ruins):**
  - Canonical Bounds: `[520, 653) × [85, 210) px` ($W: 133\text{ px}, H: 125\text{ px}$)
  - *Full Hillside Ridge (behind HQ):* Bounds `[440, 670) × [80, 250) px` ($W: 230\text{ px}, H: 170\text{ px}$)
- **Normalized Coordinates (Columns):**
  - $X$: `0.5078` (50.78%), $Y$: `0.1660` (16.60%), $W$: `0.1299` (12.99%), $H$: `0.2441` (24.41%)
- **Center Point:** $X_{\text{center}}$: `586.5 px` (57.28%), $Y_{\text{center}}$: `147.5 px` (28.81%)
- **Alignment:** Top of hill ridge at $Y = 85\text{ px}$; base obscured by headquarters roofline.
- **Spatial Relationships:**
  - Nearest Elements: Holographic license (`E06`) to the left; Login card (`E10`) to the right; HQ (`E03`) in front.
  - Overlapping Elements: Wireframe Jordan map HUD (`E06c`) overlays in front of the westernmost columns (shared bounds: `[520, 570) × [85, 140) px`).
  - Elements Behind: Starry celestial night sky (`E14`).
  - Elements In Front: Headquarters complex (`E03`) occludes the lower hill.
- **Visual Priority:** Critical (Jordanian Cultural Heritage Anchor)
- **Depth Layer:** Layer 2 (3D Deep Background)
- **Measurement Method:** Warm floodlight color scanning and column edge detection.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 3 \text{ px}$ (rocky hill contours).
- **Notes:** Columns exhibit golden floodlighting (`#fbbf24`), contrasting sharply with the deep blue sky.

---

### Element 06: Floating Holographic Jordanian Driving License
- **Element ID:** `E06-HOLO-LICENSE`
- **Element Name:** Futuristic Semi-Transparent Driving License Card, Emitter Cone & Jordan Vector Map
- **Reference Evidence:** Glowing cyan/blue rectangular card with 3D perspective pitch/yaw, portrait box, demographic lines, coat of arms, conical projection rings below, and wireframe Jordan map to right.
- **Visual Zone:** Zone D (Holographic Projection Area)
- **Bounding Box:**
  - Holographic Card Frame (`E06a`): Canonical Bounds `[322, 480) × [40, 198) px` ($W: 158\text{ px}, H: 158\text{ px}$)
  - Projection Emitter Cone / Rings (`E06b`): Canonical Bounds `[358, 446) × [160, 226) px` ($W: 88\text{ px}, H: 66\text{ px}$)
  - Wireframe Jordan Map HUD (`E06c`): Canonical Bounds `[490, 570) × [35, 140) px` ($W: 80\text{ px}, H: 105\text{ px}$)
- **Normalized Coordinates (Card Frame):**
  - $X$: `0.3145` (31.45%), $Y$: `0.0781` (7.81%), $W$: `0.1543` (15.43%), $H$: `0.3086` (30.86%)
- **Center Point:** $X_{\text{center}}$: `401 px` (39.16%), $Y_{\text{center}}$: `119 px` (23.24%)
- **Alignment:** Centered directly over headquarters portico; perspective tilt: $\sim -12^\circ$ pitch, $\sim +15^\circ$ yaw.
- **Implementation Status:**
  - *Reference Design:* Floating luminous 3D holographic projection hovering in the sky.
  - *Current Codebase Implementation:* 2D SVG/CSS HUD component (`HolographicLicense.tsx`) rendered in the DOM presentation layer.
  - *Future Technical Consideration:* In-scene 3D WebGL projection plane with custom shader is `PROPOSED` for Phase 06/08, subject to explicit stakeholder sign-off.
- **Spatial Relationships:**
  - Nearest Elements: Left branding headline to the left (clearance: ~16 px); Citadel ruins to the right (clearance: ~40 px); HQ entrance below.
  - Overlap with Citadel Columns: Wireframe Jordan map HUD (Bounds: `[490, 570) × [35, 140) px`) shares a 2D bounding region with the columns (Bounds: `[520, 570) × [85, 140) px`). This is an intentional semi-transparent overlay, not a collision.
- **Visual Priority:** Critical (Signature Institutional Digital Transformation Motif — Mandatory)
- **Depth Layer:** Visual Depth Layer 4 (Mid-Sky Holographic Projection)
- **Measurement Method:** Cyan luminescent border detection and concentric ring ellipse tracing.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 2 \text{ px}$
- **Notes:** The holographic Jordanian driving license is a mandatory requirement of the project charter.

---

### Element 07: Foreground Luxury Sedan
- **Element ID:** `E07-SEDAN-VEHICLE`
- **Element Name:** Dark Metallic Luxury Sedan with LED Taillight Bar & Jordan License Plate
- **Reference Evidence:** Modern executive sports sedan viewed from rear three-quarter angle on wet roadway.
- **Visual Zone:** Zone C (Foreground Automotive)
- **Bounding Box (Total Vehicle Silhouette):**
  - Canonical Bounds: `[188, 376) × [334, 438) px` ($W: 188\text{ px}, H: 104\text{ px}$)
  - *Sub-elements:*
    - Taillight LED Bar: Bounds `[194, 346) × [360, 380) px` ($W: 152\text{ px}, H: 20\text{ px}$)
    - License Plate ("7-9942"): Bounds `[228, 262) × [384, 398) px` ($W: 34\text{ px}, H: 14\text{ px}$)
    - Tire Road Contact Points: Left rear tire: $(x = 195\text{ px}, y = 432\text{ px})$; Right rear tire: $(x = 358\text{ px}, y = 436\text{ px})$
- **Normalized Coordinates:**
  - $X$: `0.1836` (18.36%), $Y$: `0.6523` (65.23%), $W$: `0.1836` (18.36%), $H$: `0.2031` (20.31%)
- **Center Point:** $X_{\text{center}}$: `282 px` (27.54%), $Y_{\text{center}}$: `386 px` (75.39%)
- **Alignment:** Angled along roadway curvature toward the building entrance.
- **Spatial Relationships:**
  - Nearest Elements: Three Pillars text (`E02b`) to the left ($x_{\text{max}} = 190\text{ px}$ vs car $x_{\text{min}} = 188\text{ px}$, but at different Y levels: text $Y \in [430, 475)$, car roof $Y \in [334, 430)$); HQ building behind; Wet road below.
  - Overlapping Elements: Tires occlude road surface; body partially overlaps lower-left HQ facade.
  - Elements Behind: Headquarters left wing facade.
- **Visual Priority:** High (Grounding Automotive Context)
- **Depth Layer:** Layer 5 (3D Foreground Environment)
- **Measurement Method:** Taillight emission and tire-contact shadow scanning.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 2 \text{ px}$
- **Notes:** Crimson taillights create specular reflections downward on the wet asphalt.

---

### Element 08: Wet Reflective Asphalt Roadway
- **Element ID:** `E08-WET-ROADWAY`
- **Element Name:** Wet Specular Asphalt Pavement with Light Streaks & Puddles
- **Reference Evidence:** Glossy ground plane reflecting building lights, car tail lamps, and neon curbs.
- **Visual Zone:** Zone C (Foreground Ground Plane)
- **Bounding Box:**
  - Canonical Interval: `[0, 1024) px` $\times$ `[360, 512) px`
  - $x_{\text{min}}$: `0 px`, $x_{\text{max}}$: `1024 px`, Width: `1024 px`
  - $y_{\text{min}}$: `360 px`, $y_{\text{max}}$: `512 px`, Height: `152 px`
  - Pixel Index Range: `[0..1023]` $\times$ `[360..511]`
- **Normalized Coordinates:**
  - $X$: `0.0000` (0.0%), $Y$: `0.7031` (70.31%), $W$: `1.0000` (100.0%), $H$: `0.2969` (29.69%)
- **Center Point:** (512 px, 436 px)
- **Alignment:** Spans 100% of horizontal viewport across the lower ~30% of canvas.
- **Spatial Relationships:**
  - Ground anchor for sedan (`E07`), building plaza (`E03`), and neon curbs (`E09`).
- **Visual Priority:** Critical (Cinematic Depth & Specular Realism Foundation)
- **Depth Layer:** Layer 5 (3D Scene Environment Ground Plane)
- **Measurement Method:** Ground plane horizon line tracking.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 4 \text{ px}$ (gentle horizon perspective blend).
- **Notes:** Reflections extend vertically downward toward the bottom edge.

---

### Element 09: Illuminated Roadside Curbs & Landscaping
- **Element ID:** `E09-NEON-CURBS`
- **Element Name:** Continuous Cyan Neon LED Curb Strip & Roadside Foliage
- **Reference Evidence:** Sweeping luminous cyan lines tracing the road margins with low desert shrubs.
- **Visual Zone:** Zone C (Foreground Road Framing)
- **Bounding Box (Curving Bounds):**
  - Canonical Combined Interval: `[0, 650) px` $\times$ `[360, 480) px` ($W: 650\text{ px}, H: 120\text{ px}$)
  - Left Neon Curb Curve: Canonical Bounds `[0, 220) px` $\times$ `[360, 480) px` ($W: 220\text{ px}, H: 120\text{ px}$)
  - Right Island / Perimeter Curb: Canonical Bounds `[420, 650) px` $\times$ `[365, 415) px` ($W: 230\text{ px}, H: 50\text{ px}$)
- **Normalized Coordinates (Combined Extent):**
  - $X$: `0.0000` (0.0%), $Y$: `0.7031` (70.31%), $W$: `0.6348` (63.48%), $H$: `0.2344` (23.44%)
- **Alignment:** Curving perspective splines framing the roadway.
- **Spatial Relationships:**
  - Emits directional cyan light cast onto the wet asphalt.
- **Visual Priority:** Medium-High (Urban Design Atmosphere)
- **Depth Layer:** Layer 5 (3D Scene Environment)
- **Measurement Method:** Cyan saturated spline tracing.
- **Measurement Confidence:** `MEDIUM`
- **Uncertainty:** $\pm 5 \text{ px}$ (curve thickness and glow diffusion).
- **Notes:** Guides viewer eye from lower-left into center building plaza.

---

### Element 10: Right-Side Glassmorphism Login Panel Outer Frame
- **Element ID:** `E10-LOGIN-FRAME`
- **Element Name:** Floating Frosted Glass Container with Cyan Glow Rim
- **Reference Evidence:** Large rounded rectangle with translucent dark blue fill and cyan border stroke.
- **Visual Zone:** Zone E (Right Authentication Panel)
- **Bounding Box (Solid Glass Card Outline):**
  - Canonical Interval: `[654, 986) px` $\times$ `[52, 466) px`
  - $x_{\text{min}}$: `654 px`, $x_{\text{max}}$: `986 px`, Width: `332 px`
  - $y_{\text{min}}$: `52 px`, $y_{\text{max}}$: `466 px`, Height: `414 px`
  - *Outer Diffuse Cyan Halo:* `[630, 1010) px` $\times$ `[32, 486) px` (W: 380 px, H: 454 px)
  - *Corner Radius:* $\sim 24 \text{ px}$ ($r / W \approx 7.2\%$)
- **Normalized Coordinates (Solid Frame):**
  - $X$: `0.6387` (63.87%), $Y$: `0.1016` (10.16%), $W$: `0.3242` (32.42%), $H$: `0.8086` (80.86%)
- **Center Point:** $X_{\text{center}}$: `820 px` (80.08%), $Y_{\text{center}}$: `259 px` (50.59%)
- **Alignment & Margins:**
  - Left Margin: `654 px`
  - Right Margin to Canvas: `38 px` ($1024 - 986 = 38\text{ px}$, $3.71\%$)
  - Top Margin to Canvas: `52 px` ($10.16\%$)
  - Bottom Margin to Canvas: `46 px` ($512 - 466 = 46\text{ px}$, $8.98\%$)
  - *Note on Historical Notation:* Under discrete pixel-index notation, the card's final column was pixel index 985 and row 465. In continuous geometric bounds, the right and bottom edges are $x_{\text{max}} = 986\text{ px}$ and $y_{\text{max}} = 466\text{ px}$.
- **Spatial Relationships:**
  - Nearest Elements: HQ right canopy to the left (clearance gap: 14 px from canopy edge $x_{\text{max}} = 640\text{ px}$ to card border $x_{\text{min}} = 654\text{ px}$, clearance interval $[640, 654)\text{ px}$); Citadel ruins behind; Right screen edge to the right.
  - Elements Behind: 3D scene background recedes into deep shadow behind the card to maximize legibility.
- **Visual Priority:** Critical (Primary User Authentication Container)
- **Depth Layer:** Layer 6C (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** Direct cross-sectional edge scan at Y = 256 and X = 820.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 1 \text{ px}$
- **Notes:** Sits as a dominant 2D presentation card over the right ~33% of the screen.

---

### Element 11: Security Crest Emblem & Card Header
- **Element ID:** `E11-CARD-HEADER`
- **Element Name:** Hashemite Security Crown & Shield Emblem, "Welcome back" Title & Subtitle
- **Reference Evidence:** Glowing cyan concentric ring security crest and crisp typography.
- **Visual Zone:** Zone E (Right Authentication Panel)
- **Bounding Box:**
  - Canonical Header Combined Interval: `[715, 920) px` $\times$ `[68, 222) px` ($W: 205\text{ px}, H: 154\text{ px}$)
  - Security Crest Emblem: Canonical Bounds `[765, 875) px` $\times$ `[68, 155) px` ($W: 110\text{ px}, H: 87\text{ px}$)
  - "Welcome back" Title: Canonical Bounds `[724, 916) px` $\times$ `[168, 192) px` ($W: 192\text{ px}, H: 24\text{ px}$)
  - Explanatory Subtitle: Canonical Bounds `[720, 920) px` $\times$ `[198, 222) px` ($W: 200\text{ px}, H: 24\text{ px}$)
- **Normalized Coordinates:**
  - Crest Emblem: $X$: `0.7471` (74.71%), $Y$: `0.1328` (13.28%), $W$: `0.1074` (10.74%), $H$: `0.1699` (16.99%)
  - Header Text Group: $X$: `0.7031` (70.31%), $Y$: `0.3281` (32.81%), $W$: `0.1953` (19.53%), $H$: `0.1055` (10.55%)
- **Center Point:** Center X of all header elements: `820 px` (Card vertical centerline).
- **Alignment:** Strictly horizontally centered within the login card frame.
- **Spatial Relationships:**
  - Top padding inside card: 16 px (card top 52 to crest top 68).
  - Gap from subtitle to username field: 14 px (222 to 236).
- **Visual Priority:** High (Security Reassurance & Welcome Hook)
- **Depth Layer:** Layer 6C (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** High-contrast cyan/white edge scanning.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 1 \text{ px}$
- **Notes:** Crest emblem includes Hashemite crown silhouette inside concentric glowing HUD rings.

---

### Element 12: Login Credentials Form & Primary Action
- **Element ID:** `E12-LOGIN-FORM`
- **Element Name:** Username Input, Password Input, Remember Me, Forgot Password, and Sign-In Button
- **Reference Evidence:** Rounded translucent input boxes with icons, checkbox, and vibrant cyan-blue action button.
- **Visual Zone:** Zone E (Right Authentication Panel)
- **Bounding Box:**
  - Canonical Form Block Combined Interval: `[676, 962) px` $\times$ `[236, 372) px` ($W: 286\text{ px}, H: 136\text{ px}$)
  - Username Input Field: Canonical Bounds `[676, 962) px` $\times$ `[236, 266) px` ($W: 286\text{ px}, H: 30\text{ px}$)
  - Password Input Field: Canonical Bounds `[676, 962) px` $\times$ `[278, 308) px` ($W: 286\text{ px}, H: 30\text{ px}$)
  - Remember Me & Forgot Password Row: Canonical Bounds `[676, 962) px` $\times$ `[316, 330) px` ($W: 286\text{ px}, H: 14\text{ px}$)
  - "Sign in ->" Primary Button: Canonical Bounds `[676, 962) px` $\times$ `[340, 372) px` ($W: 286\text{ px}, H: 32\text{ px}$)
- **Normalized Coordinates (Combined Form Block):**
  - $X$: `0.6602` (66.02%), $Y$: `0.4609` (46.09%), $W$: `0.2793` (27.93%), $H$: `0.2656` (26.56%)
- **Center Point:** $X_{\text{center}}$: `819 px` (79.98%), $Y_{\text{center}}$: `304 px` (59.38%)
- **Alignment:** Horizontal form width = 286 px; card side padding = 22 px (from card border $x_{\text{min}} = 654\text{ px}$ to input $x_{\text{min}} = 676\text{ px}$, margin interval $[654, 676)\text{ px}$).
- **Spatial Relationships:**
  - Vertical spacing between inputs: 12 px (from input 1 $y_{\text{max}} = 266\text{ px}$ to input 2 $y_{\text{min}} = 278\text{ px}$, interval $[266, 278)\text{ px}$).
  - Vertical spacing between password and Remember Me row: 8 px (interval $[308, 316)\text{ px}$).
  - Vertical spacing between Remember Me and Sign-in button: 10 px (interval $[330, 340)\text{ px}$).
- **Visual Priority:** Critical (Core Authentication Workflow)
- **Depth Layer:** Layer 6C (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** Input border contrast scan and button gradient measurement.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 1 \text{ px}$
- **Notes:** Full-width action button (286 px) matches exact input width.

---

### Element 13: Auxiliary SSO Authentication & Security Notice
- **Element ID:** `E13-CARD-AUXILIARY`
- **Element Name:** "OR CONTINUE WITH" Divider, Microsoft/SSO/QR Buttons & Security Notice
- **Reference Evidence:** Horizontal rule with text divider, three pill buttons, and green shield token badge.
- **Visual Zone:** Zone E (Right Authentication Panel)
- **Bounding Box:**
  - Canonical Auxiliary Block Combined Interval: `[676, 962) px` $\times$ `[388, 464) px` ($W: 286\text{ px}, H: 76\text{ px}$)
  - "OR CONTINUE WITH" Divider: Canonical Bounds `[750, 888) px` $\times$ `[388, 400) px` ($W: 138\text{ px}, H: 12\text{ px}$)
  - SSO Buttons Row: Canonical Bounds `[676, 962) px` $\times$ `[408, 438) px` ($W: 286\text{ px}, H: 30\text{ px}$)
    - Microsoft Button: Canonical Bounds `[676, 764) px` $\times$ `[408, 438) px` ($W: 88\text{ px}, H: 30\text{ px}$)
    - SSO Button: Canonical Bounds `[775, 863) px` $\times$ `[408, 438) px` ($W: 88\text{ px}, H: 30\text{ px}$)
    - QR Code Button: Canonical Bounds `[874, 962) px` $\times$ `[408, 438) px` ($W: 88\text{ px}, H: 30\text{ px}$)
    - (Inter-button gaps: $[764, 775)\text{ px}$ and $[863, 874)\text{ px}$, each $11\text{ px}$)
  - Security Notice ("Protected by secure token-based authentication."): Canonical Bounds `[708, 932) px` $\times$ `[450, 464) px` ($W: 224\text{ px}, H: 14\text{ px}$)
- **Normalized Coordinates:**
  - $X$: `0.6602` (66.02%), $Y$: `0.7578` (75.78%), $W$: `0.2793` (27.93%), $H$: `0.1504` (15.04%)
- **Center Point:** $X_{\text{center}}$: `819 px`, $Y_{\text{center}}$: `426 px`
- **Alignment:** Aligned to the 286 px form grid; centered notice.
- **Spatial Relationships:**
  - Bottom padding inside card: 2 px to inner frame boundary ($y_{\text{max}} = 466\text{ px}$ vs notice bottom $y_{\text{max}} = 464\text{ px}$); 22 px to diffuse halo bottom ($y = 486\text{ px}$).
- **Visual Priority:** Medium-High (Enterprise Security Features)
- **Depth Layer:** Layer 6C (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** Component edge tracing.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 1 \text{ px}$
- **Notes:** Complete symmetry around vertical centerline $x = 819\text{--}820\text{ px}$.

---

### Element 14: Cinematic Night Atmosphere & Horizon Radiosity
- **Element ID:** `E14-CINEMATIC-ATMOSPHERE`
- **Element Name:** Deep Celestial Navy Sky, Atmospheric Haze, Horizon Gradient, and Starfield
- **Reference Evidence:** Midnight blue gradient, fine distant stars, horizon glow, and high-rise skyline lights on the far right.
- **Visual Zone:** Zone F (Global Atmosphere)
- **Bounding Box:** Full Canvas (`[0, 1024) px` $\times$ `[0, 512) px`)
  - Sky Area: Canonical Bounds `[0, 1024) px` $\times$ `[0, 360) px`
  - Distant Skyline Lights (Right Horizon): Canonical Bounds `[960, 1024) px` $\times$ `[280, 380) px`
- **Normalized Coordinates:** $X \in [0.0, 1.0]$, $Y \in [0.0, 1.0]$
- **Alignment:** Full canvas backdrop.
- **Spatial Relationships:**
  - Unifies all middle-ground and foreground assets through cool moonlight and warm amber bounce.
- **Visual Priority:** Critical (Global Atmospheric Unifier)
- **Depth Layer:** Layer 1 (3D Scene Environment Celestial Dome)
- **Measurement Method:** Color space luminance profiling.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 0 \text{ px}$ (global canvas).
- **Notes:** Darkest celestial areas at top center have luminance $\approx 5\text{--}8\%$; golden Citadel columns reach luminance $\approx 75\text{--}85\%$.

---

## 4. Master Measurement Summary Table

| ID | Element Name | Zone | Canonical Bounds $[x_{\text{min}}, x_{\text{max}}) \times [y_{\text{min}}, y_{\text{max}})\text{ px}$ | Width (px) | Height (px) | Norm X | Norm Y | Norm W | Norm H | Confidence |
|---|---|:---:|:---:|---:|---:|---:|---:|---:|---:|:---:|
| `E01` | Left Branding Header Group | A | `[76, 300) × [43, 118)` | 224 | 75 | 0.0742 | 0.0840 | 0.2188 | 0.1465 | `HIGH` |
| `E02` | Slogan & Mission Copy Block | A | `[76, 336) × [134, 248)` | 260 | 114 | 0.0742 | 0.2617 | 0.2539 | 0.2227 | `HIGH` |
| `E02b`| Three Pillar Badges | A | `[76, 190) × [430, 475)` | 114 | 45 | 0.0742 | 0.8398 | 0.1113 | 0.0879 | `HIGH` |
| `E03` | Headquarters Architectural Complex | B | `[195, 640) × [200, 400)` | 445 | 200 | 0.1904 | 0.3906 | 0.4346 | 0.3906 | `HIGH` |
| `E04a`| English "DVLD" Facade Sign | B | `[393, 449) × [250, 285)` | 56 | 35 | 0.3838 | 0.4883 | 0.0547 | 0.0684 | `HIGH` |
| `E04b`| Arabic Canopy Signage | B | `[495, 625) × [285, 330)` | 130 | 45 | 0.4834 | 0.5566 | 0.1270 | 0.0879 | `HIGH` |
| `E05` | Amman Citadel (Temple of Hercules) | B/F | `[520, 653) × [85, 210)` | 133 | 125 | 0.5078 | 0.1660 | 0.1299 | 0.2441 | `HIGH` |
| `E06a`| Holographic Driving License Card | D | `[322, 480) × [40, 198)` | 158 | 158 | 0.3145 | 0.0781 | 0.1543 | 0.3086 | `HIGH` |
| `E06b`| Holographic Emitter Cone / Rings | D | `[358, 446) × [160, 226)` | 88 | 66 | 0.3496 | 0.3125 | 0.0859 | 0.1289 | `HIGH` |
| `E06c`| Wireframe Jordan Map HUD | D | `[490, 570) × [35, 140)` | 80 | 105 | 0.4785 | 0.0684 | 0.0781 | 0.2051 | `HIGH` |
| `E07` | Foreground Luxury Sedan | C | `[188, 376) × [334, 438)` | 188 | 104 | 0.1836 | 0.6523 | 0.1836 | 0.2031 | `HIGH` |
| `E07b`| License Plate ("7-9942") | C | `[228, 262) × [384, 398)` | 34 | 14 | 0.2227 | 0.7500 | 0.0332 | 0.0273 | `HIGH` |
| `E08` | Wet Reflective Asphalt Roadway | C | `[0, 1024) × [360, 512)` | 1024 | 152 | 0.0000 | 0.7031 | 1.0000 | 0.2969 | `HIGH` |
| `E09` | Neon Curbs & Perimeter Lighting | C | `[0, 650) × [360, 480)` | 650 | 120 | 0.0000 | 0.7031 | 0.6348 | 0.2344 | `MEDIUM` |
| `E10` | Login Glass Card Outer Frame | E | `[654, 986) × [52, 466)` | 332 | 414 | 0.6387 | 0.1016 | 0.3242 | 0.8086 | `HIGH` |
| `E11` | Security Crest Emblem & Title | E | `[715, 920) × [68, 222)` | 205 | 154 | 0.6982 | 0.1328 | 0.1953 | 0.3008 | `HIGH` |
| `E12` | Credentials Form (Inputs & Button) | E | `[676, 962) × [236, 372)` | 286 | 136 | 0.6602 | 0.4609 | 0.2793 | 0.2656 | `HIGH` |
| `E13` | Auxiliary SSO & Token Notice | E | `[676, 962) × [388, 464)` | 286 | 76 | 0.6602 | 0.7578 | 0.2793 | 0.1484 | `HIGH` |
| `E14` | Global Celestial Atmosphere | F | `[0, 1024) × [0, 512)` | 1024 | 512 | 0.0000 | 0.0000 | 1.0000 | 1.0000 | `HIGH` |

---

## 5. Spatial Clearances, Negative Space & Overlap Register

### 5.1 Rigorous Spatial Clearance Analysis
1. **Left Branding Text vs Central Headquarters Entrance Portico:**
   - *Description Text Canonical Bounds:* `[76, 336) × [210, 248) px` ($W: 260\text{ px}, H: 38\text{ px}$).
   - *Headquarters Bright Central Entrance Portico Canonical Bounds:* `[380, 480) × [215, 390) px` ($W: 100\text{ px}, H: 175\text{ px}$).
   - *Measured Clearance Gap:* **$44\text{ px}$ ($4.30\%$ canvas width)** from text right boundary ($x_{\text{max}} = 336\text{ px}$) to entrance glass frame ($x_{\text{min}} = 380\text{ px}$, interval $[336, 380)\text{ px}$).
   - *Clarification on Left Building Wing:* The overall headquarters bounding box begins at $x_{\text{min}} = 195\text{ px}$. However, the westernmost wing (Canonical Bounds `[195, 335) × [200, 385) px`) is deeply recessed and shadowed, providing a high-contrast dark architectural backdrop directly behind the description text. There is zero legibility collision because the text does not intersect the brightly lit central architectural portal.
2. **Headquarters Canopy to Right Login Card:**
   - *Right Canopy Fascia Edge:* $x_{\text{max}} = 640\text{ px}$.
   - *Solid Login Card Border:* $x_{\text{min}} = 654\text{ px}$.
   - *Measured Clearance Gap:* **$14\text{ px}$ ($1.37\%$ canvas width)** (clearance interval $[640, 654)\text{ px}$).
   - *Requirement:* The building architecture and illuminated Arabic signage terminate completely before reaching the solid border of the login card.
3. **Holographic License Corridors & Overlap Analysis:**
   - *Holographic Card Left Clearance:* Bounded horizontally at $[322, 480)\text{ px}$, maintaining $16\text{ px}$ clearance from left branding copy ($x_{\text{max}} = 306\text{ px}$, clearance interval $[306, 322)\text{ px}$).
   - *Wireframe Jordan Map HUD vs Citadel Columns Overlap:*
     - Jordan Map HUD Canonical Bounds: `[490, 570) × [35, 140) px`.
     - Citadel Roman Columns Canonical Bounds: `[520, 653) × [85, 210) px`.
     - *Shared Overlap Region:* `[520, 570) × [85, 140) px` ($W: 50\text{ px}$ horizontal span, $H: 55\text{ px}$ vertical span).
     - *Analysis:* This represents an **intentional semi-transparent holographic overlay**. The wireframe Jordan map is rendered with fine cyan vector lines that overlay directly in front of the illuminated amber columns without occluding them.
4. **Login Card Viewport Margins (Continuous Geometric Bounds):**
   - Left Margin: $654\text{ px}$ ($63.87\%$)
   - Top Margin: $52\text{ px}$ ($10.16\%$)
   - Right Margin to Canvas: $1024 - 986 = \mathbf{38\text{ px}}$ ($3.71\%$)
   - Bottom Margin to Canvas: $512 - 466 = \mathbf{46\text{ px}}$ ($8.98\%$)
   - *(Note: Legacy discrete pixel indexing previously cited 39 px and 47 px by subtracting inclusive pixel indices 985 and 465 from canvas limits).*

### 5.2 Intentional Overlap Register
| Front Element | Behind Element | Overlap Region | Purpose & Nature | Confidence |
|---|---|---|---|:---:|
| **Jordan Map HUD (`E06c`)** | **Citadel Columns (`E05`)** | `[520, 570) × [85, 140) px` | Intentional semi-transparent vector overlay; HUD projects in front of background ruins | `HIGH` |
| **Left Copy (`E02`)** | **HQ Left Wing (`E03`)** | `[195, 336) × [210, 248) px` | 2D text overlays in front of darkened, unlit architectural facade wing | `HIGH` |
| **Luxury Sedan (`E07`)** | **HQ Left Wing (`E03`)** | `[195, 335) × [334, 385) px` | Physical depth layering; sedan grounds the architectural plaza | `HIGH` |
| **Luxury Sedan (`E07`)** | **Road Plane (`E08`)** | `[188, 376) × [432, 438) px` | Tire contact shadow on wet asphalt creates physical grounding | `HIGH` |
| **HQ Complex (`E03`)** | **Citadel Hill (`E05`)** | `[440, 640) × [200, 250) px` | Historic Citadel naturally looms on the hill behind the modern building | `HIGH` |
| **Emitter Cone (`E06b`)**| **HQ Portico Roof (`E03`)** | `[358, 446) × [215, 226) px` | Projector base originates from building canopy roof | `HIGH` |
| **Login Card (`E10`)** | **Horizon Skyline (`E14`)** | `[654, 986) × [52, 466) px` | 2D UI presentation card floats over darkened 3D background | `HIGH` |

---

## 6. Depth and Layer Hierarchy (Pure Visual Depth Order)

> [!IMPORTANT]
> The hierarchy below reflects purely visual depth ordering observed in the 2D reference composition. It does **not** specify physical world-space distances or Three.js camera metrics.

```text
Visual Depth Order (Back to Front):
Layer 1: Background Celestial Sky & Distant Horizon Stars
   │
Layer 2: Historic Amman Citadel & Temple of Hercules (Deep Background Ridge)
   │
Layer 3: DVLD Modern Headquarters Architectural Complex (Middle-Ground)
   │
Layer 4: Holographic Driving License & Projection Emitter (Mid-Sky Layer)
   │
Layer 5: Foreground Wet Roadway, Neon Curbs & Luxury Sedan (Foreground Environment)
   │
Layer 6: Interactive 2D Presentation Layer (HTML / CSS in DOM)
         ├─ 6A: Left Institutional Branding & Mission Copy
         ├─ 6B: Holographic Driving License SVG HUD (Current DOM Implementation)
         └─ 6C: Glassmorphic Authentication Card (Form, Inputs, SSO)
```

---

## 7. Responsive Viewport Planning Analysis (Proposed Design Hypotheses)

> [!NOTE]
> All responsive recommendations below represent **PROPOSED DESIGN HYPOTHESES** formulated during Phase 01 optical analysis. They are planning considerations for future implementation phases and do **not** represent verified measurements or user-approved policy.

| Target Viewport | Aspect Ratio | Vertical Delta vs 2:1 | Proposed Spatial Behavior | Risk / Clearance Concern |
|---|:---:|:---:|---|---|
| **1920 × 1080** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` 3D Camera vertical FOV expands downwards, extending road plane downward ($Y \in [0.70, 1.00]$ canvas height). Left branding and login card maintain reference margins ($x_{\text{min}} = 76\text{ px}$ and $x_{\text{max}} = 986\text{ px}$ equivalent, margin $38\text{ px}$). | Zero collision risk; ample breathing room above building. |
| **1600 × 900** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` Proportional scaling identical to 1080p; maintain $14\text{ px}$ minimum clearance between card border and building canopy. | Maintain clearance between card and canopy. |
| **1366 × 768** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` Card width scales down to $320\text{ px}$; left margin adjusts to $48\text{ px}$; 3D camera maintains centered lookAt target. | Monitor left description copy line breaks. |
| **1280 × 720** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` Headline font scales down slightly (e.g. 32px); form inputs compact to 34 to 36 px height; text copy maintains 3-line wrap. | Left text must not drift toward center building. |
| **1024 × 768** | 4 : 3 (1.33 : 1) | +50.0% vertical height | `PROPOSED:` Extreme horizontal compression: camera dollies back slightly; left branding stacks above plaza; or horizontal scroll disabled with centered card priority. | High risk of overlap if 3D scene is not adjusted dynamically via R3F camera aspect handler. |

---

## 8. Diagnostic Evidence Record

- **Master Reference Image:** `media_1793445589710_98905759.jpg` (SHA-256: `c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0`)
- **Diagnostic Overlay Path:** [`ClientApp/docs/cinematic-login/evidence/phase-01/diagnostic_bounding_boxes.png`](file:///d:/DVLD-WPF/ClientApp/docs/cinematic-login/evidence/phase-01/diagnostic_bounding_boxes.png)
- **Status:** `ANALYSIS ONLY — NOT AN APPLICATION ASSET`
- **Resolution:** 1024 x 512 pixels
