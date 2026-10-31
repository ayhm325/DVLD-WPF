# DVLD Cinematic Login — Spatial Composition & Layout Specification

> **Document ID:** `03-COMPOSITION-SPEC`  
> **Status:** AUDITED & EMPIRICALLY MEASURED — AUTHORITATIVE SPECIFICATION  
> **Authoritative For:** Spatial Layout, Element Bounding Boxes, Depth Layers, Clearances, and Viewport Planning  
> **Master Visual Reference:** `media_1793445589710_98905759.jpg` (Original Approved Reference Image)  
> **Reference Hash Verification:** SHA-256 `c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0` (Identical bit-for-bit to `media_1793448307234_76e52d5e.jpg`)  
> **Measurement Baseline:** 1024 x 512 pixels (2.0000 : 1 aspect ratio)

---

## 1. Reference Canvas Geometry & Master Coordinate System

| Metric | Measured Reference Finding | Conversion / Specification |
|---|---|---|
| **Canvas Dimensions** | 1024 x 512 pixels | Measured from JPEG header SOF0 marker |
| **Aspect Ratio** | 2.0000 : 1 | Ultrawide panoramic cinematic ratio |
| **Master Origin (0,0)** | Top-Left Corner | X: 0 to 1024 (Left to Right), Y: 0 to 512 (Top to Bottom) |
| **Normalized Coordinates** | $X_{\text{norm}} \in [0.0, 1.0]$, $Y_{\text{norm}} \in [0.0, 1.0]$ | $X_{\text{norm}} = X / 1024$, $Y_{\text{norm}} = Y / 512$ |
| **Percentage Coordinates** | $X\% \in [0.0\%, 100.0\%]$, $Y\% \in [0.0\%, 100.0\%]$ | $X\% = (X / 1024) \times 100$, $Y\% = (Y / 512) \times 100$ |
| **Width / Height Scale** | $W\% = (W / 1024) \times 100$, $H\% = (H / 512) \times 100$ | Horizontal and vertical percentages are independent |

---

## 2. Master Visual Zones

The reference composition partitions the 1024 x 512 canvas into six interconnected visual zones:

```text
+----------------------------------------------------------------------------------------------------+
| Zone F: Celestial Night Sky & Atmosphere (Y: 0 - 220)                                              |
|                                                                                                    |
| Zone A: Left Branding       Zone D: Holographic License    Zone B: Citadel Ruins    Zone E:        |
| - Chevron Logo              - Floating Card (322,40)       - Roman Columns (520,85) | Glassmorphic |
| - DVLD Title (126,45)       - Emitter Cone (358,160)       - Rocky Hill Ridge       | Login Card   |
| - Slogan (76,134)           - Jordan Map Wireframe (490,35)                         | Frame        |
| - Description (76,210)                                                              | (654, 52)    |
|                             Zone B: Central Headquarters Complex                    | to           |
|                             - Glass Facade, Lobby & Entrance Portico (195,200)      | (985, 465)   |
|                             - English Sign "DVLD" (393,250)                         |              |
| Zone C: Vehicle & Road      - Arabic Signage (495,285)                              | Width: 332px |
| - Luxury Sedan (188,334)                                                            | Height:414px |
| - Wet Reflective Roadway Plane (Full Width, Y: 360 - 512)                           |              |
| - Curving Neon Curbs (Lower-Left to Center-Right)                                   |              |
+----------------------------------------------------------------------------------------------------+
```

### Zone A: Left Branding & Institutional Identity (X: 76 – 336 px, Y: 43 – 475 px)
- Houses institutional identity, mission copy, and core value statements.
- Occupies the left-hand column (~25.4% canvas width); text overlays over the darkened, shadowed western facade of the headquarters complex without visual collision.

### Zone B: Central Architectural Complex (X: 195 – 653 px, Y: 85 – 400 px)
- Centerpiece modern multi-story glass-and-steel headquarters building, illuminated lobbies, entrance canopy, and bilingual signage.
- Historic Amman Citadel and Temple of Hercules columns rise directly behind the building on the elevated rocky hill ridge.

### Zone C: Foreground Roadway & Automotive (X: 0 – 1024 px, Y: 334 – 512 px)
- Ground plane covered by wet reflective asphalt reflecting ambient lighting, vehicle tail lamps, and neon curbs.
- Foreground luxury sports sedan positioned at an angle on the road (rear three-quarters).

### Zone D: Holographic License Digital Transformation Area (X: 322 – 570 px, Y: 35 – 226 px)
- Luminous cyan floating driver's license card projecting into the mid-sky above the headquarters portico.
- Anchored by concentric conical emitter rings originating from the building entrance, accompanied by a semi-transparent wireframe Jordan map HUD to the right.

### Zone E: Right Interactive Authentication Panel (X: 654 – 985 px, Y: 52 – 465 px)
- Prominent floating frosted glass container (332 x 414 px, 32.42% x 80.86% canvas).
- Houses Hashemite security crest, welcome header, credentials inputs, remember me, sign in button, SSO buttons, and legal footer.

### Zone F: Global Atmospheric Environment (Full Canvas, Y: 0 – 360 px)
- Nocturnal starry sky, atmospheric depth haze, distant city horizon lights (X: 960 to 1024), and global radiosity.

---

## 3. Element Measurement Register (All 14 Mandatory Elements)

### Element 01: Left-Side DVLD Institutional Branding Group
- **Element ID:** `E01-BRAND-HEADER`
- **Element Name:** DVLD Institutional Logo, Title, Subtitle, and Operations Pill Badge
- **Reference Evidence:** High-contrast white and cyan vector typography and stacked chevron icon.
- **Visual Zone:** Zone A (Left Branding)
- **Bounding Box (Overall Group):**
  - X: `76 px`, Y: `43 px`, Width: `224 px`, Height: `75 px`
  - *Sub-elements:*
    - Stacked Chevron Logo: X: `76 px`, Y: `43 px`, W: `33 px`, H: `36 px`
    - "DVLD" Acronym Title: X: `126 px`, Y: `45 px`, W: `84 px`, H: `31 px`
    - Subtitle ("Driving & Vehicle Licensing Department"): X: `126 px`, Y: `75 px`, W: `174 px`, H: `13 px`
    - Active Status Badge ("Licensing operations platform"): X: `76 px`, Y: `97 px`, W: `140 px`, H: `21 px`
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
  - X: `76 px`, Y: `134 px`, Width: `260 px`, Height: `114 px`
  - *Sub-elements:*
    - Headline ("Driving forward. / Managing smarter."): X: `76 px`, Y: `134 px`, W: `230 px`, H: `64 px`
    - Explanatory Description: X: `76 px`, Y: `210 px`, W: `260 px`, H: `38 px`
    - Lower Three Pillars ("Safer Roads. / Smarter Services. / A Better Tomorrow."): X: `76 px`, Y: `430 px`, W: `114 px`, H: `45 px`
- **Normalized Coordinates:**
  - Headline/Desc: $X$: `0.0742` (7.42%), $Y$: `0.2617` (26.17%), $W$: `0.2539` (25.39%), $H$: `0.2227` (22.27%)
  - Three Pillars: $X$: `0.0742` (7.42%), $Y$: `0.8398` (83.98%), $W$: `0.1113` (11.13%), $H$: `0.0879` (8.79%)
- **Center Point:** Headline/Desc: (206 px, 191 px); Pillars: (133 px, 452.5 px)
- **Alignment:** Strictly flush-left to X = 76 px.
- **Spatial Relationships:**
  - Nearest Elements: Central headquarters entrance portico (`E03`) to the right (horizontal clearance: 44 px).
  - Background Context: The text block overlays the shadowed, unlit western wing of the headquarters building facade (X: 195 to 335 px), which provides high-contrast dark backing.
- **Visual Priority:** High (Brand Mission)
- **Depth Layer:** Layer 6A (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** Optical contrast edge scanning.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 2 \text{ px}$
- **Notes:** The gap between the rightmost extent of the description (X = 336 px) and the bright central entrance portal (X = 380 px) is 44 px.

---

### Element 03: Central Modern Glass-and-Steel Headquarters Complex
- **Element ID:** `E03-HQ-ARCHITECTURE`
- **Element Name:** Modern DVLD Architectural Headquarters Complex
- **Reference Evidence:** Multi-level glass curtain-wall structure with structural pillars, illuminated interior offices, and entrance portico.
- **Visual Zone:** Zone B (Central Architecture)
- **Bounding Box (Total Building Silhouette):**
  - X: `195 px`, Y: `200 px`, Width: `445 px`, Height: `200 px`
  - *Key Structural Sub-regions:*
    - Left Structural Wing (Recessed/Shadowed): X: `195 px` to `335 px`, Y: `240 px` to `380 px`
    - Central Entrance Portico / Lobby (Brightly Lit): X: `340 px` to `480 px`, Y: `215 px` to `390 px`
    - Right Canopy & Extended Wing: X: `480 px` to `640 px`, Y: `260 px` to `385 px`
- **Normalized Coordinates:**
  - $X$: `0.1904` (19.04%), $Y$: `0.3906` (39.06%), $W$: `0.4346` (43.46%), $H$: `0.3906` (39.06%)
- **Center Point:** $X_{\text{center}}$: `417.5 px` (40.77%), $Y_{\text{center}}$: `300 px` (58.59%)
- **Alignment:** Anchored to middle-ground ground line (Y ~ 385–390 px).
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
  - English "DVLD" Facade Sign: X: `393 px`, Y: `250 px`, Width: `56 px`, Height: `35 px`
  - Arabic Canopy Sign (`دائرة ترخيص السواقين والمركبات`): X: `495 px`, Y: `285 px`, Width: `130 px`, Height: `45 px`
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
- **Notes:** Signage is physically anchored to the building model.

---

### Element 05: Amman Citadel & Temple of Hercules Ruins
- **Element ID:** `E05-CITADEL-RUINS`
- **Element Name:** Ancient Roman Fluted Columns, Architrave & Rocky Jabal al-Qal'a Hill
- **Reference Evidence:** Warm amber-lit Roman fluted columns and limestone archaeological ruins on the natural hillside.
- **Visual Zone:** Zone B / Zone F (Background Heritage Backdrop)
- **Bounding Box (Columns & Upper Ruins):**
  - X: `520 px`, Y: `85 px`, Width: `133 px`, Height: `125 px`
  - *Full Hillside Ridge (behind HQ):* X: `440 px` to `670 px`, Y: `80 px` to `250 px`
- **Normalized Coordinates (Columns):**
  - $X$: `0.5078` (50.78%), $Y$: `0.1660` (16.60%), $W$: `0.1299` (12.99%), $H$: `0.2441` (24.41%)
- **Center Point:** $X_{\text{center}}$: `586.5 px` (57.28%), $Y_{\text{center}}$: `147.5 px` (28.81%)
- **Alignment:** Top of hill ridge at Y ~ 85 px; base obscured by headquarters roofline.
- **Spatial Relationships:**
  - Nearest Elements: Holographic license (`E06`) to the left; Login card (`E10`) to the right; HQ (`E03`) in front.
  - Overlapping Elements: Wireframe Jordan map HUD (`E06c`) overlays in front of the westernmost columns (shared X: 520–570 px, Y: 85–140 px).
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
  - Holographic Card Frame: X: `322 px`, Y: `40 px`, Width: `158 px`, Height: `158 px`
  - Projection Emitter Cone / Rings: X: `358 px`, Y: `160 px`, Width: `88 px`, Height: `66 px`
  - Wireframe Jordan Map HUD: X: `490 px`, Y: `35 px`, Width: `80 px`, Height: `105 px`
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
  - Overlap with Citadel Columns: Wireframe Jordan map HUD ($X: 490\text{--}570\text{ px}$, $Y: 35\text{--}140\text{ px}$) shares a 2D bounding region with the columns ($X: 520\text{--}570\text{ px}$, $Y: 85\text{--}140\text{ px}$). This is an intentional semi-transparent overlay, not a collision.
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
  - X: `188 px`, Y: `334 px`, Width: `188 px`, Height: `104 px`
  - *Sub-elements:*
    - Taillight LED Bar: X: `194 px`, Y: `360 px`, W: `152 px`, H: `20 px`
    - License Plate ("7-9942"): X: `228 px`, Y: `384 px`, W: `34 px`, H: `14 px`
    - Tire Road Contact: Left rear tire: (X: 195, Y: 432); Right rear tire: (X: 358, Y: 436)
- **Normalized Coordinates:**
  - $X$: `0.1836` (18.36%), $Y$: `0.6523` (65.23%), $W$: `0.1836` (18.36%), $H$: `0.2031` (20.31%)
- **Center Point:** $X_{\text{center}}$: `282 px` (27.54%), $Y_{\text{center}}$: `386 px` (75.39%)
- **Alignment:** Angled along roadway curvature toward the building entrance.
- **Spatial Relationships:**
  - Nearest Elements: Three Pillars text (`E02b`) to the left ($X = 190\text{ px}$ vs car $X = 188\text{ px}$, but at different Y levels: text $Y: 430\text{--}475$, car roof $Y: 334\text{--}430$); HQ building behind; Wet road below.
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
  - X: `0 px`, Y: `360 px`, Width: `1024 px`, Height: `152 px`
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
  - Left Neon Curb Curve: X: `0 px` to `220 px`, Y: `360 px` to `480 px`
  - Right Island / Perimeter Curb: X: `420 px` to `650 px`, Y: `365 px` to `415 px`
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
  - X: `654 px`, Y: `52 px`, Width: `332 px`, Height: `414 px`
  - *Outer Diffuse Cyan Halo:* X: `630 px` to `1010 px`, Y: `32 px` to `486 px` (W: 380 px, H: 454 px)
  - *Corner Radius:* $\sim 24 \text{ px}$ ($r / W \approx 7.2\%$)
- **Normalized Coordinates (Solid Frame):**
  - $X$: `0.6387` (63.87%), $Y$: `0.1016` (10.16%), $W$: `0.3242` (32.42%), $H$: `0.8086` (80.86%)
- **Center Point:** $X_{\text{center}}$: `820 px` (80.08%), $Y_{\text{center}}$: `259 px` (50.59%)
- **Alignment:** Right-aligned with a 39 px right margin (from X = 985 to 1024); vertically centered (top margin 52 px, bottom margin 47 px).
- **Spatial Relationships:**
  - Nearest Elements: HQ right canopy to the left (clearance gap: 14 px from canopy edge X = 640 to card border X = 654); Citadel ruins behind; Right screen edge to the right.
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
  - Security Crest Emblem: X: `765 px`, Y: `68 px`, Width: `110 px`, Height: `87 px`
  - "Welcome back" Title: X: `724 px`, Y: `168 px`, Width: `192 px`, Height: `24 px`
  - Explanatory Subtitle: X: `720 px`, Y: `198 px`, Width: `200 px`, Height: `24 px`
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
  - Username Input Field: X: `676 px`, Y: `236 px`, Width: `286 px`, Height: `30 px`
  - Password Input Field: X: `676 px`, Y: `278 px`, Width: `286 px`, Height: `30 px`
  - Remember Me & Forgot Password Row: X: `676 px`, Y: `316 px`, Width: `286 px`, Height: `14 px`
  - "Sign in ->" Primary Button: X: `676 px`, Y: `340 px`, Width: `286 px`, Height: `32 px`
- **Normalized Coordinates (Combined Form Block):**
  - $X$: `0.6602` (66.02%), $Y$: `0.4609` (46.09%), $W$: `0.2793` (27.93%), $H$: `0.2656` (26.56%)
- **Center Point:** $X_{\text{center}}$: `819 px` (79.98%), $Y_{\text{center}}$: `304 px` (59.38%)
- **Alignment:** Horizontal form width = 286 px; card side padding = 22 px (from card border X = 654 to input X = 676).
- **Spatial Relationships:**
  - Vertical spacing between inputs: 12 px (Y: 266 to 278).
  - Vertical spacing between password and Remember Me row: 8 px (Y: 308 to 316).
  - Vertical spacing between Remember Me and Sign-in button: 10 px (Y: 330 to 340).
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
  - "OR CONTINUE WITH" Divider: X: `750 px`, Y: `388 px`, Width: `138 px`, Height: `12 px`
  - SSO Buttons Row (3 equal buttons): X: `676 px`, Y: `408 px`, Width: `286 px`, Height: `30 px`
    - Microsoft Button: X: `676 px`, W: `88 px`, H: `30 px`
    - SSO Button: X: `775 px`, W: `88 px`, H: `30 px`
    - QR Code Button: X: `874 px`, W: `88 px`, H: `30 px`
    - (Inter-button gap: 11 px)
  - Security Notice ("Protected by secure token-based authentication."): X: `708 px`, Y: `450 px`, Width: `224 px`, Height: `14 px`
- **Normalized Coordinates:**
  - $X$: `0.6602` (66.02%), $Y$: `0.7578` (75.78%), $W$: `0.2793` (27.93%), $H$: `0.1504` (15.04%)
- **Center Point:** $X_{\text{center}}$: `819 px`, $Y_{\text{center}}$: `426 px`
- **Alignment:** Aligned to the 286 px form grid; centered notice.
- **Spatial Relationships:**
  - Bottom padding inside card: 15 px (from notice bottom Y = 464 to card bottom Y = 465, with glow halo).
- **Visual Priority:** Medium-High (Enterprise Security Features)
- **Depth Layer:** Layer 6C (Interactive HTML/CSS Presentation Layer)
- **Measurement Method:** Component edge tracing.
- **Measurement Confidence:** `HIGH`
- **Uncertainty:** $\pm 1 \text{ px}$
- **Notes:** Complete symmetry around vertical axis X = 819–820 px.

---

### Element 14: Cinematic Night Atmosphere & Horizon Radiosity
- **Element ID:** `E14-CINEMATIC-ATMOSPHERE`
- **Element Name:** Deep Celestial Navy Sky, Atmospheric Haze, Horizon Gradient, and Starfield
- **Reference Evidence:** Midnight blue gradient, fine distant stars, horizon glow, and high-rise skyline lights on the far right.
- **Visual Zone:** Zone F (Global Atmosphere)
- **Bounding Box:** Full Canvas (`X: 0 to 1024 px`, `Y: 0 to 512 px`)
  - Sky Area: Y: `0 px` to `360 px`
  - Distant Skyline Lights (Right Horizon): X: `960 px` to `1024 px`, Y: `280 px` to `380 px`
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

| ID | Element Name | Zone | X (px) | Y (px) | Width (px) | Height (px) | Norm X | Norm Y | Norm W | Norm H | Confidence |
|---|---|:---:|---:|---:|---:|---:|---:|---:|---:|---:|:---:|
| `E01` | Left Branding Header Group | A | 76 | 43 | 224 | 75 | 0.0742 | 0.0840 | 0.2188 | 0.1465 | `HIGH` |
| `E02` | Slogan & Mission Copy Block | A | 76 | 134 | 260 | 114 | 0.0742 | 0.2617 | 0.2539 | 0.2227 | `HIGH` |
| `E02b`| Three Pillar Badges | A | 76 | 430 | 114 | 45 | 0.0742 | 0.8398 | 0.1113 | 0.0879 | `HIGH` |
| `E03` | Headquarters Architectural Complex | B | 195 | 200 | 445 | 200 | 0.1904 | 0.3906 | 0.4346 | 0.3906 | `HIGH` |
| `E04a`| English "DVLD" Facade Sign | B | 393 | 250 | 56 | 35 | 0.3838 | 0.4883 | 0.0547 | 0.0684 | `HIGH` |
| `E04b`| Arabic Canopy Signage | B | 495 | 285 | 130 | 45 | 0.4834 | 0.5566 | 0.1270 | 0.0879 | `HIGH` |
| `E05` | Amman Citadel (Temple of Hercules) | B/F | 520 | 85 | 133 | 125 | 0.5078 | 0.1660 | 0.1299 | 0.2441 | `HIGH` |
| `E06a`| Holographic Driving License Card | D | 322 | 40 | 158 | 158 | 0.3145 | 0.0781 | 0.1543 | 0.3086 | `HIGH` |
| `E06b`| Holographic Emitter Cone / Rings | D | 358 | 160 | 88 | 66 | 0.3496 | 0.3125 | 0.0859 | 0.1289 | `HIGH` |
| `E06c`| Wireframe Jordan Map HUD | D | 490 | 35 | 80 | 105 | 0.4785 | 0.0684 | 0.0781 | 0.2051 | `HIGH` |
| `E07` | Foreground Luxury Sedan | C | 188 | 334 | 188 | 104 | 0.1836 | 0.6523 | 0.1836 | 0.2031 | `HIGH` |
| `E07b`| License Plate ("7-9942") | C | 228 | 384 | 34 | 14 | 0.2227 | 0.7500 | 0.0332 | 0.0273 | `HIGH` |
| `E08` | Wet Reflective Asphalt Roadway | C | 0 | 360 | 1024 | 152 | 0.0000 | 0.7031 | 1.0000 | 0.2969 | `HIGH` |
| `E09` | Neon Curbs & Perimeter Lighting | C | 0 | 360 | 650 | 120 | 0.0000 | 0.7031 | 0.6348 | 0.2344 | `MEDIUM` |
| `E10` | Login Glass Card Outer Frame | E | 654 | 52 | 332 | 414 | 0.6387 | 0.1016 | 0.3242 | 0.8086 | `HIGH` |
| `E11` | Security Crest Emblem & Title | E | 715 | 68 | 200 | 154 | 0.6982 | 0.1328 | 0.1953 | 0.3008 | `HIGH` |
| `E12` | Credentials Form (Inputs & Button) | E | 676 | 236 | 286 | 136 | 0.6602 | 0.4609 | 0.2793 | 0.2656 | `HIGH` |
| `E13` | Auxiliary SSO & Token Notice | E | 676 | 388 | 286 | 76 | 0.6602 | 0.7578 | 0.2793 | 0.1484 | `HIGH` |
| `E14` | Global Celestial Atmosphere | F | 0 | 0 | 1024 | 512 | 0.0000 | 0.0000 | 1.0000 | 1.0000 | `HIGH` |

---

## 5. Spatial Clearances, Negative Space & Overlap Register

### 5.1 Rigorous Spatial Clearance Analysis
1. **Left Branding Text vs Central Headquarters Entrance Portico:**
   - *Description Text Bounding Box:* X: `76 px` to `336 px`, Y: `210 px` to `248 px`.
   - *Headquarters Bright Central Entrance Portico:* X: `380 px` to `480 px`, Y: `215 px` to `390 px`.
   - *Measured Clearance Gap:* **$44\text{ px}$ ($4.30\%$ canvas width)** from text right edge ($X = 336$) to entrance glass frame ($X = 380$).
   - *Clarification on Left Building Wing:* The overall headquarters bounding box begins at $X = 195\text{ px}$. However, the westernmost wing (X: 195 to 335 px) is deeply recessed and shadowed, providing a high-contrast dark architectural backdrop directly behind the description text. There is zero legibility collision because the text does not intersect the brightly lit central architectural portal.
2. **Headquarters Canopy to Right Login Card:**
   - *Right Canopy Fascia Edge:* $X = 640\text{ px}$.
   - *Solid Login Card Border:* $X = 654\text{ px}$.
   - *Measured Clearance Gap:* **$14\text{ px}$ ($1.37\%$ canvas width)**.
   - *Requirement:* The building architecture and illuminated Arabic signage terminate completely before reaching the solid border of the login card.
3. **Holographic License Corridors & Overlap Analysis:**
   - *Holographic Card Left Clearance:* Bounded horizontally at $X = 322\text{ px}$ to $480\text{ px}$, maintaining $16\text{ px}$ clearance from left branding copy ($X = 306\text{ px}$).
   - *Wireframe Jordan Map HUD vs Citadel Columns Overlap:*
     - Jordan Map HUD Bounding Box: $X: 490\text{--}570\text{ px}$, $Y: 35\text{--}140\text{ px}$.
     - Citadel Roman Columns Bounding Box: $X: 520\text{--}653\text{ px}$, $Y: 85\text{--}210\text{ px}$.
     - *Shared Overlap Region:* $X: 520\text{--}570\text{ px}$ ($51\text{ px}$ horizontal span), $Y: 85\text{--}140\text{ px}$ ($56\text{ px}$ vertical span).
     - *Analysis:* This represents an **intentional semi-transparent holographic overlay**. The wireframe Jordan map is rendered with fine cyan vector lines that overlay directly in front of the illuminated amber columns without occluding them.
4. **Login Card Viewport Margins:**
   - Top Margin: $52\text{ px}$ ($10.16\%$)
   - Right Margin: $39\text{ px}$ ($3.81\%$)
   - Bottom Margin: $47\text{ px}$ ($9.18\%$)

### 5.2 Intentional Overlap Register
| Front Element | Behind Element | Overlap Region | Purpose & Nature | Confidence |
|---|---|---|---|:---:|
| **Jordan Map HUD (`E06c`)** | **Citadel Columns (`E05`)** | X: 520–570 px, Y: 85–140 px | Intentional semi-transparent vector overlay; HUD projects in front of background ruins | `HIGH` |
| **Left Copy (`E02`)** | **HQ Left Wing (`E03`)** | X: 195–336 px, Y: 210–248 px | 2D text overlays in front of darkened, unlit architectural facade wing | `HIGH` |
| **Luxury Sedan (`E07`)** | **HQ Left Wing (`E03`)** | X: 195–335 px, Y: 334–385 px | Physical depth layering; sedan grounds the architectural plaza | `HIGH` |
| **Luxury Sedan (`E07`)** | **Road Plane (`E08`)** | X: 188–376 px, Y: 432–438 px | Tire contact shadow on wet asphalt creates physical grounding | `HIGH` |
| **HQ Complex (`E03`)** | **Citadel Hill (`E05`)** | X: 440–640 px, Y: 200–250 px | Historic Citadel naturally looms on the hill behind the modern building | `HIGH` |
| **Emitter Cone (`E06b`)**| **HQ Portico Roof (`E03`)** | X: 358–446 px, Y: 215–226 px | Projector base originates from building canopy roof | `HIGH` |
| **Login Card (`E10`)** | **Horizon Skyline (`E14`)** | X: 654–985 px, Y: 52–465 px | 2D UI presentation card floats over darkened 3D background | `HIGH` |

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
| **1920 × 1080** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` 3D Camera vertical FOV expands downwards, extending road plane from Y: 70% to 100%. Left branding and login card maintain reference margins ($X = 76\text{ px}$ and $X_{\text{right}} = 985\text{ px}$ equivalent). | Zero collision risk; ample breathing room above building. |
| **1600 × 900** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` Proportional scaling identical to 1080p; maintain $14\text{ px}$ minimum clearance between card border and building canopy. | Maintain clearance between card and canopy. |
| **1366 × 768** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` Card width scales down to $320\text{ px}$; left margin adjusts to $48\text{ px}$; 3D camera maintains centered lookAt target. | Monitor left description copy line breaks. |
| **1280 × 720** | 16 : 9 (1.78 : 1) | +12.5% vertical height | `PROPOSED:` Headline font scales down slightly (e.g. 32px); form inputs compact to 34–36px height; text copy maintains 3-line wrap. | Left text must not drift toward center building. |
| **1024 × 768** | 4 : 3 (1.33 : 1) | +50.0% vertical height | `PROPOSED:` Extreme horizontal compression: camera dollies back slightly; left branding stacks above plaza; or horizontal scroll disabled with centered card priority. | High risk of overlap if 3D scene is not adjusted dynamically via R3F camera aspect handler. |

---

## 8. Diagnostic Evidence Record

- **Master Reference Image:** `media_1793445589710_98905759.jpg` (SHA-256: `c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0`)
- **Diagnostic Overlay Path:** [`ClientApp/docs/cinematic-login/evidence/phase-01/diagnostic_bounding_boxes.png`](file:///d:/DVLD-WPF/ClientApp/docs/cinematic-login/evidence/phase-01/diagnostic_bounding_boxes.png)
- **Status:** `ANALYSIS ONLY — NOT AN APPLICATION ASSET`
- **Resolution:** 1024 x 512 pixels
