# DVLD Cinematic Login — Visual Design System & Design Tokens

> **Document ID:** `02-DESIGN-SYSTEM`  
> **Phase:** 02.1 — Accuracy & Completeness Correction  
> **Status:** AUDITED & EMPIRICALLY MEASURED — SPECIFICATION SPEC  
> **Authoritative For:** Color Palette, Value Origins, Typography Hierarchy, Spacing Scale, 1080p Scaling Methodology, Glassmorphism, Lighting Language, Full Component Specifications, and WCAG Contrast Verification  
> **Visual Source of Truth:** `media_1793445589710_98905759.jpg` (SHA-256: `c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0`)  
> **Measurement Baseline:** 1024 × 512 reference pixel space and Phase 01.1 spatial composition specification

---

## 1. Design System Architecture & Token Taxonomy

The DVLD design system establishes a semantic design-token architecture connecting 2D interactive UI controls with the 3D cinematic nocturnal environment.

### 1.1 Classification of Values (Reference-derived vs Proposed)
To maintain engineering integrity, all values in this design system are explicitly separated into two categories:
1. **Reference-derived Values:** Values directly measured, sampled, or calculated from the approved reference raster image (`1024 × 512`). These represent verified visual facts of the reference artifact.
2. **Proposed Values:** Engineering and aesthetic starting points selected for implementation. These include:
   - Target viewport dimensions (1080p and responsive viewports).
   - Glassmorphic parameters (backdrop blur radius, saturation multipliers, border opacity, composite surface opacity).
   - Radius values (card, button, and input corner radii).
   - Shadow formulas (blur radius, spread, ambient drop-shadow offsets).
   - 3D lighting parameters (color temperatures in Kelvin, luminous intensities, falloff distances).
   - Motion and transition parameters (durations, easing curves, translation offsets).
   - Typography font families and line-heights.
   - Semantic token role assignments (mapping a sampled color to a component role).

```text
Token Architecture:
  color.background.*       (Canvas sky, surface backdrops, elevation fills)
  color.brand.*            (Electric cyan, cobalt blue, Hashemite gold, carmine red)
  color.text.*             (Primary white, secondary muted slate, cyan accent)
  color.border.*           (Glassmorphism strokes, subtle input dividers)
  color.status.*           (Security green, error crimson, warning amber)
  typography.*             (Font family candidates, font size, weight, line-height)
  spacing.*                (4px base grid scale and component-specific gaps)
  radius.*                 (Card, input, button, and pill corner radii)
  glass.*                  (Backdrop blur, opacity, border glow, shadow formulas)
  lighting.*               (Emissive cyan neon, golden amber floodlights, taillight red)
  motion.*                 (Durations, easing functions, reduced-motion behavior)
```

---

## 2. Color System

### 2.1 Color Palette Register

There are **exactly 23 color tokens** defined in the authoritative register.

Every token distinguishes between its **Value Origin** (whether the raw HEX/RGB was sampled from the reference image or proposed for UI completeness) and its **Semantic Role Status** (the assignment of that color to a specific UI role is an engineering proposal):

| # | Token Name | HEX & RGB Equivalent | Value Origin | Reference Source Location / Sampling Method | Semantic Role / Intended Usage | Value Status | Semantic Status | Confidence |
|:---:|---|---|:---:|---|---|:---:|:---:|:---:|
| 1 | `color.background.zenith` | `#00102C`<br>`rgb(0, 16, 44)` | Reference-derived | Sampled: Sky zenith ($X: 500\text{--}520, Y: 10\text{--}25$) | Deepest celestial upper sky dome | `SAMPLED` | `PROPOSED` | `HIGH` |
| 2 | `color.background.base` | `#010311`<br>`rgb(1, 3, 17)` | Reference-derived | Sampled: Upper mid sky ($X: 200\text{--}220, Y: 40\text{--}55$) | Nocturnal atmospheric dark base | `SAMPLED` | `PROPOSED` | `HIGH` |
| 3 | `color.background.horizon` | `#C7F1FF`<br>`rgb(199, 241, 255)` | Reference-derived | Sampled: Horizon glow above HQ roofline | Distant cyan atmospheric horizon haze & moonlight | `SAMPLED` | `PROPOSED` | `HIGH` |
| 4 | `color.brand.cyan.core` | `#6BDDFF`<br>`rgb(107, 221, 255)` | Reference-derived | Sampled: Left Chevron Logo ($X: 85\text{--}95, Y: 50\text{--}65$) | Primary interactive cyan, HUD lines & logo | `SAMPLED` | `PROPOSED` | `HIGH` |
| 5 | `color.brand.cyan.accent` | `#64C0F1`<br>`rgb(100, 192, 241)` | Reference-derived | Sampled: Slogan text ($X: 130\text{--}160, Y: 180\text{--}190$) | Headline cyan accent, pills & indicators | `SAMPLED` | `PROPOSED` | `HIGH` |
| 6 | `color.brand.cyan.neon` | `#16B3FF`<br>`rgb(22, 179, 255)` | Reference-derived | Sampled: Road curb strip ($X: 91, Y: 408$) | Emissive curb strip, card glow & focus rings | `SAMPLED` | `PROPOSED` | `HIGH` |
| 7 | `color.brand.blue.action` | `#178BFC`<br>`rgb(23, 139, 252)` | Reference-derived | Sampled: Button left edge ($X: 680, Y: 356$) | Primary "Sign in ->" button gradient start | `SAMPLED` | `PROPOSED` | `HIGH` |
| 8 | `color.brand.blue.deep` | `#1849FB`<br>`rgb(24, 73, 251)` | Reference-derived | Sampled: Button right mid ($X: 910, Y: 356$) | Primary "Sign in ->" button gradient end | `SAMPLED` | `PROPOSED` | `HIGH` |
| 9 | `color.surface.card.base` | `#001331`<br>`rgb(0, 19, 49)` | Reference-derived | Sampled: Card interior ($X: 810\text{--}830, Y: 240\text{--}260$) | Login glass card dark backdrop base fill | `SAMPLED` | `PROPOSED` | `HIGH` |
| 10 | `color.surface.card.rim` | `#DFFAFF`<br>`rgb(223, 250, 255)` | Reference-derived | Sampled: Card left border ($X: 654, Y: 200\text{--}220$) | Glass card perimeter border highlight stroke | `SAMPLED` | `PROPOSED` | `HIGH` |
| 11 | `color.surface.card.glow` | `#3A47AD`<br>`rgb(58, 71, 173)` | Reference-derived | Sampled: Card outer margin ($X: 638\text{--}648, Y: 200\text{--}220$) | Glass card diffuse cyan-blue outer aura | `SAMPLED` | `PROPOSED` | `HIGH` |
| 12 | `color.surface.input.fill` | `#212D50`<br>`rgb(33, 45, 80)` | Reference-derived | Sampled: Username input field interior | Input field translucent background fill | `SAMPLED` | `PROPOSED` | `HIGH` |
| 13 | `color.surface.input.border` | `#13274A`<br>`rgb(19, 39, 74)` | Reference-derived | Sampled: Username input border edge | Input field subtle perimeter border stroke | `SAMPLED` | `PROPOSED` | `HIGH` |
| 14 | `color.surface.sso.fill` | `#0A203E`<br>`rgb(10, 32, 62)` | Reference-derived | Sampled: SSO button ($X: 700\text{--}720, Y: 415\text{--}425$) | Auxiliary SSO pill button surface fill | `SAMPLED` | `PROPOSED` | `HIGH` |
| 15 | `color.architectural.amber` | `#FD7E2F`<br>`rgb(253, 126, 47)` | Reference-derived | Sampled: Temple of Hercules ($X: 600, Y: 176$) | Roman columns golden floodlight glow | `SAMPLED` | `PROPOSED` | `HIGH` |
| 16 | `color.architectural.warm` | `#FFF6E1`<br>`rgb(255, 246, 225)` | Reference-derived | Sampled: HQ lobby ($X: 360\text{--}380, Y: 300$) | Headquarters interior warm lobby lights | `SAMPLED` | `PROPOSED` | `HIGH` |
| 17 | `color.automotive.red` | `#D60927`<br>`rgb(214, 9, 39)` | Reference-derived | Sampled: Sedan taillight ($X: 327, Y: 379$) | Sedan LED taillight core strip | `SAMPLED` | `PROPOSED` | `HIGH` |
| 18 | `color.automotive.refl` | `#FC0F61`<br>`rgb(252, 15, 97)` | Reference-derived | Sampled: Road reflection ($X: 252, Y: 449$) | Wet road specular red reflection streak | `SAMPLED` | `PROPOSED` | `HIGH` |
| 19 | `color.text.primary` | `#FFFFFF`<br>`rgb(255, 255, 255)` | Reference-derived | Sampled: Headline ($X: 80\text{--}100, Y: 155$) | Primary titles, slogans & button labels | `SAMPLED` | `PROPOSED` | `HIGH` |
| 20 | `color.text.secondary` | `#D5D8E9`<br>`rgb(213, 216, 233)` | Reference-derived | Sampled: Description ($X: 100\text{--}150, Y: 220$) | Explanatory copy, subtitles & metadata | `SAMPLED` | `PROPOSED` | `HIGH` |
| 21 | `color.text.muted` | `#94A3B8`<br>`rgb(148, 163, 184)` | Proposed | Synthetic UI candidate (Neutral Slate) | Input placeholders & legal notices | `SYNTHETIC` | `PROPOSED` | `MEDIUM` |
| 22 | `color.status.success` | `#218259`<br>`rgb(33, 130, 89)` | Reference-derived | Sampled: Shield badge ($X: 723, Y: 445$) | Security notice verified shield icon & badge | `SAMPLED` | `PROPOSED` | `HIGH` |
| 23 | `color.status.error` | `#EF4444`<br>`rgb(239, 68, 68)` | Proposed | Synthetic UI candidate (Standard Red-500) | Form validation errors & failure borders | `SYNTHETIC` | `PROPOSED` | `MEDIUM` |

---

## 3. Typography System

### 3.1 Font Family Strategy
- **Western Sans-Serif (English Text):**
  - *Reference Status:* Geometric/Neo-Grotesque Sans-Serif (`UNKNOWN FONT FAMILY`).
  - *Proposed Candidates:* `Inter`, `Plus Jakarta Sans`, system `Segoe UI`.
- **Arabic Typography (Signage & Localized Text):**
  - *Reference Status:* Modern Architectural Kufic / Geometric Arabic (`UNKNOWN FONT FAMILY`).
  - *Proposed Candidates:* `Cairo`, `Tajawal`, `IBM Plex Sans Arabic`.
- **Monospace (License Identification & Codes):**
  - *Proposed Candidates:* `JetBrains Mono` or `Space Mono` for license numbers (`30-8942-019`).

### 3.2 Typographic Hierarchy & Specifications

> [!NOTE]
> Reference sizes in $1024 \times 512$ are **Reference-derived** (estimated from raster letterform heights). All Target 1080p sizes, font family candidates, line heights, and letter spacings are **Proposed**.

| Role / Element | Reference 1024x512 Size | Target 1080p Size | Proposed Font Family | Weight | Line Height | Letter Spacing | Color Token | Value Status |
|---|:---:|:---:|---|:---:|:---:|:---:|---|:---:|
| **Logo Acronym ("DVLD")** | ~24 px | ~40 px | Inter / Plus Jakarta | Bold (700) | 1.1 | -0.02em | `color.text.primary` | `PROPOSED` |
| **Department Subtitle** | ~8 px | ~13 px | Inter | Regular (400) | 1.3 | 0.00em | `color.text.secondary` | `PROPOSED` |
| **Status Badge Pill** | ~8 px | ~12 px | Inter | Medium (500) | 1.0 | +0.02em | `color.brand.cyan.accent` | `PROPOSED` |
| **Display Headline Line 1** | ~20 px | ~36 px | Inter | Bold (700) | 1.15 | -0.02em | `color.text.primary` | `PROPOSED` |
| **Display Headline Line 2** | ~20 px | ~36 px | Inter | Bold (700) | 1.15 | -0.02em | `color.brand.cyan.accent` | `PROPOSED` |
| **Mission Paragraph** | ~9 px | ~14 px | Inter | Regular (400) | 1.5 | 0.00em | `color.text.secondary` | `PROPOSED` |
| **Three Lower Mission Statements** | ~8 px | ~12 px | Inter | Medium (500) | 1.6 | +0.01em | `color.text.secondary` | `PROPOSED` |
| **Login "Welcome"** | ~18 px | ~30 px | Inter | Regular (400) | 1.2 | -0.01em | `color.text.primary` | `PROPOSED` |
| **Login "back"** | ~18 px | ~30 px | Inter | Bold (700) | 1.2 | -0.01em | `color.brand.cyan.accent` | `PROPOSED` |
| **Card Subtitle** | ~9 px | ~13 px | Inter | Regular (400) | 1.4 | 0.00em | `color.text.secondary` | `PROPOSED` |
| **Input Labels / Placeholder** | ~9 px | ~14 px | Inter | Regular (400) | 1.0 | 0.00em | `color.text.muted` | `PROPOSED` |
| **Primary Button ("Sign in ->")** | ~10 px | ~15 px | Inter | Semi-bold (600) | 1.0 | +0.02em | `color.text.primary` | `PROPOSED` |
| **SSO Button Labels** | ~8 px | ~12 px | Inter | Medium (500) | 1.0 | +0.01em | `color.text.secondary` | `PROPOSED` |
| **Security Footer Notice** | ~7.5 px | ~11 px | Inter | Regular (400) | 1.3 | 0.00em | `color.text.secondary` | `PROPOSED` |
| **Arabic Canopy Signage** | ~18 px | ~32 px | Cairo / Tajawal | Semi-bold (600) | 1.2 | 0.00em | `color.surface.card.rim` | `PROPOSED` |
| **Holographic License Header** | ~9 px | ~14 px | Inter / JetBrains Mono | Bold (700) | 1.0 | +0.05em | `color.brand.cyan.core` | `PROPOSED` |
| **Holographic License Number** | ~7 px | ~11 px | JetBrains Mono | Medium (500) | 1.0 | +0.02em | `color.text.primary` | `PROPOSED` |
| **Footer Legal & Tagline** | ~7 px | ~11 px | Inter | Regular (400) | 1.3 | 0.00em | `color.text.secondary` | `PROPOSED` |

---

## 4. Spacing, Sizing, and 1080p Scaling Methodology

### 4.1 The Aspect Ratio Discrepancy & Scaling Mathematics
A critical mathematical discrepancy exists between the reference image and standard web viewports:
- **Reference Resolution:** $1024 \times 512\text{ px}$, Aspect Ratio = $2.0 : 1$ (panoramic ultrawide).
- **Target Standard Desktop:** $1920 \times 1080\text{ px}$, Aspect Ratio = $16:9 \approx 1.7778 : 1$.

```text
Horizontal scaling factor:  Sx = 1920 / 1024 = 1.875x
Vertical scaling factor:    Sy = 1080 / 512  = 2.109375x
Aspect Ratio Ratio (Sy/Sx): 2.109375 / 1.875 = 1.125  (+12.5% vertical height)
```

Because 16:9 is **12.5% taller** than 2.0:1, applying a single uniform multiplier globally is geometrically impossible:
- A uniform $1.875\times$ scale leaves $120\text{ px}$ of empty space vertically ($512 \times 1.875 = 960\text{ px} \ne 1080\text{ px}$).
- A uniform $2.109375\times$ scale produces $2160\text{ px}$ width ($1024 \times 2.109375 = 2160\text{ px}$), clipping $240\text{ px}$ off the sides.

### 4.2 Documented Scaling Methodologies
To preserve visual harmony without distortion, the design system utilizes 5 explicit scaling mechanisms:

1. **Percentage-based Horizontal Positioning:**
   - Elements tied to the horizontal layout (such as the left branding margin at $7.42\%$ and the right login card alignment) scale relative to viewport width ($1920 \times 0.0742 \approx 140\text{ px}$).
2. **Independent Width and Height Adaptation:**
   - The 3D scene camera frustum ($FOV_y$) and environment plane adapt independently: the architectural skyline occupies the upper 60% of vertical space, while the foreground wet asphalt road plane expands downward to absorb the extra $12.5\%$ vertical height at 16:9.
3. **Fixed-Size UI Ergonomics Constraints:**
   - Form inputs, buttons, and text cannot scale strictly proportionally. Interactive targets are constrained by human motor ergonomics and WCAG touch targets (e.g., input height is capped at $46\text{ px}$ rather than $30 \times 2.109 = 63.3\text{ px}$; button height is fixed at $48\text{ px}$).
4. **Uniform Scaling for Graphic Marks:**
   - Self-contained vector marks (the DVLD chevron logo icon, security crest, and SSO icons) scale uniformly ($1:1$ aspect ratio) to prevent distortion.
5. **Responsive Layout Adaptations:**
   - Across standard breakpoints, the layout gracefully shifts proportions to prevent collision between the building canopy and the login card.

### 4.3 Component Sizing & Clearances (Reference vs Proposed 1080p)

| Parameter / Dimension | Reference Measured (1024x512) | Value Origin | Proposed 1080p Equivalent | Scaling Method Used | Value Status |
|---|:---:|:---:|:---:|---|:---:|
| **Left Viewport Margin (`spacing.viewport.left`)** | `76 px` ($7.42\%$) | Reference-derived | `140 px` | Percentage ($7.42\% \times 1920 \approx 142\text{px}$) | `PROPOSED` |
| **Right Viewport Margin (`spacing.viewport.right`)** | `39 px` ($3.81\%$) | Reference-derived | `72 px` | Percentage ($3.81\% \times 1920 \approx 73\text{px}$) | `PROPOSED` |
| **Top Viewport Margin (`spacing.viewport.top`)** | `52 px` ($10.16\%$) | Reference-derived | `96 px` | Independent Height ($10.16\% \times 1080 \approx 109\text{px}$) | `PROPOSED` |
| **Bottom Viewport Margin (`spacing.viewport.bottom`)** | `47 px` ($9.18\%$) | Reference-derived | `88 px` | Independent Height ($9.18\% \times 1080 \approx 99\text{px}$) | `PROPOSED` |
| **Login Card Width (`size.card.width`)** | `332 px` ($32.42\%$) | Reference-derived | `420 px` | Ergonomic UI constraint (capped at 420px) | `PROPOSED` |
| **Login Card Height (`size.card.height`)** | `414 px` ($80.86\%$) | Reference-derived | `640 px` | Ergonomic UI constraint (vertical flow) | `PROPOSED` |
| **Login Card Side Padding (`spacing.card.padding.x`)** | `22 px` | Reference-derived | `32 px` | Fixed ergonomic scale | `PROPOSED` |
| **Input Field Dimensions (`size.input`)** | `286 × 30 px` | Reference-derived | `356 × 46 px` | Fixed ergonomic touch target | `PROPOSED` |
| **Input-to-Input Gap (`spacing.form.gap`)** | `12 px` | Reference-derived | `16 px` | 4px Grid scale (`spacing.4`) | `PROPOSED` |
| **Sign-In Button Height (`size.button.height`)** | `32 px` | Reference-derived | `48 px` | Standard ergonomic button target | `PROPOSED` |
| **SSO Button Dimensions & Gap** | `88 × 30 px`, Gap `11 px` | Reference-derived | `108 × 44 px`, Gap `16 px` | Ergonomic touch target & 4px grid | `PROPOSED` |
| **Card-to-Building Canopy Clearance** | `14 px` ($1.37\%$) | Reference-derived | `26 px` | Percentage buffer ($1.37\% \times 1920 \approx 26\text{px}$) | `PROPOSED` |
| **Text-to-Portico Clearance** | `44 px` ($4.30\%$) | Reference-derived | `82 px` | Percentage buffer ($4.30\% \times 1920 \approx 82\text{px}$) | `PROPOSED` |

---

## 5. Glassmorphism System

### 5.1 Inferred Visual Properties vs Proposed CSS Specifications

All physical CSS attributes below represent **Proposed starting points**, derived from optical analysis of the reference image:

| Parameter | Inferred Visual Appearance | Proposed CSS Implementation Specification | Value Origin | Status |
|---|---|---|:---:|:---:|
| **Card Background Fill** | Deep nocturnal slate-navy with high translucency | `background: rgba(0, 19, 49, 0.72)` | Proposed | `PROPOSED` |
| **Card Backdrop Blur** | Heavy optical frosted diffusion of scene lights | `backdrop-filter: blur(20px) saturate(160%)` | Proposed | `PROPOSED` |
| **Card Border Stroke** | Delicate bright cyan outer rim stroke | `border: 1.5px solid rgba(100, 192, 241, 0.65)` | Proposed | `PROPOSED` |
| **Card Outer Glow Halo** | Broad luminous cyan-blue atmospheric aura | `box-shadow: 0 0 35px rgba(22, 179, 255, 0.30), inset 0 0 15px rgba(22, 179, 255, 0.08)` | Proposed | `PROPOSED` |
| **Card Corner Radius** | Soft modern rounded silhouette | `border-radius: 24px` | Proposed | `PROPOSED` |
| **Input Field Fill** | Translucent slate with subtle inner depth | `background: rgba(33, 45, 80, 0.45)` | Proposed | `PROPOSED` |
| **Input Field Border** | 1px subtle slate stroke; cyan glow on focus | `border: 1px solid rgba(255, 255, 255, 0.12)` | Proposed | `PROPOSED` |
| **Input Corner Radius** | Moderately rounded corners | `border-radius: 10px` | Proposed | `PROPOSED` |
| **Primary Action Button** | Linear horizontal gradient cyan to blue | `background: linear-gradient(90deg, #178BFC 0%, #0554F9 60%, #1849FB 100%)` | Proposed | `PROPOSED` |
| **Button Glow Shadow** | Directional cyan drop-shadow aura | `box-shadow: 0 4px 20px rgba(23, 139, 252, 0.45)` | Proposed | `PROPOSED` |
| **SSO Button Fill** | Dark translucent pill button with 1px border | `background: rgba(10, 32, 62, 0.50); border: 1px solid rgba(255, 255, 255, 0.08)` | Proposed | `PROPOSED` |

---

## 6. Lighting and Glow Language

The lighting system connects 2D interface controls with 3D architectural lighting:

| Lighting Effect | Color Token | Visual Purpose | Intensity / Spread (Proposed) | Color Temperature (Proposed) | Value Origin | Status |
|---|---|---|---|---|:---:|:---:|
| **Electric Cyan Rim Glow** | `color.brand.cyan.neon` | Frames login card as floating HUD | 1.5px stroke + 25px halo | $\sim 10000\text{K}$ (Cold) | Proposed | `PROPOSED` |
| **Security Crest Radial Glow** | `color.brand.cyan.core` | Government authority anchor on card | Radial falloff 40px | $\sim 9500\text{K}$ (Cold) | Proposed | `PROPOSED` |
| **Roadside Neon Lighting Strip** | `color.brand.cyan.neon` | Highway curb guiding the viewer's eye | 30px ground wash, intensity 1.2 | $\sim 12000\text{K}$ (Cold) | Proposed | `PROPOSED` |
| **Holographic License Aura** | `color.brand.cyan.core` | Conical emitter projection from roof | Volumetric cone, 45px falloff | $\sim 9000\text{K}$ (Cold) | Proposed | `PROPOSED` |
| **Architectural Amber Flood** | `color.architectural.amber` | Warm historic spotlighting on ruins | High intensity (1.8), hard shadows | $\sim 3200\text{K}$ (Warm Amber) | Proposed | `PROPOSED` |
| **Interior Lobby Radiance** | `color.architectural.warm` | Communicates active government workspace | Diffuse ambient through glass | $\sim 2800\text{K}$ (Warm White) | Proposed | `PROPOSED` |
| **Taillight Specular Streaks** | `color.automotive.red` | Grounds vehicle onto wet road plane | Sharp directional specular streak | Monochromatic Red (630nm) | Proposed | `PROPOSED` |
| **Celestial Sky Moonlight** | `color.background.horizon` | Soft cool directional fill from high angle | Intensity 0.45, soft shadows | $\sim 7500\text{K}$ (Cool Blue) | Proposed | `PROPOSED` |

---

## 7. Complete Component Coverage & Specifications

### 7.1 Floating Holographic Jordanian Driving License (`E06`)
- **Visual Purpose:** Represents smart digital governance, modernized citizen credentials, and official authority floating in 3D space above the department headquarters.
- **Hierarchy:** Secondary focal anchor after the primary login card, positioned in the upper-mid architectural sky.
- **Spatial Coordinates (Phase 01.1):**
  - Bounding Box: $X: 319\text{--}482\text{ px}$ ($31.15\%\text{--}47.07\%$, width $163\text{ px} / 15.92\%$), $Y: 70\text{--}194\text{ px}$ ($13.67\%\text{--}37.89\%$, height $124\text{ px} / 24.22\%$).
  - 3D Orientation: Pitched backward $\sim 15^\circ$ with subtle counter-clockwise yaw.
  - Conical Emitter Projection: Base at roof canopy ($X \approx 390\text{--}410, Y \approx 194\text{--}230$), projecting 3 concentric volumetric rings expanding upwards into the card base.
- **Color Treatment:**
  - Card Perimeter Rim: `color.brand.cyan.core` (`#6BDDFF`) 2px glowing border stroke with 15px cyan halo.
  - Card Fill Plane: Translucent cyan-tinted glass (`rgba(0, 19, 49, 0.40)`).
  - Verified Status Badge: `color.status.success` (`#218259`) background pill with white checkmark and "VERIFIED" label.
  - Avatar Silhouette: Semi-transparent cyan user silhouette cutout ($X \approx 330\text{--}365, Y \approx 105\text{--}155$).
- **Typography & Data Fields (Proposed):**
  - Title: "DRIVING LICENSE" (Bold 700, ~9px in reference / proposed 14px in 1080p, `#6BDDFF`).
  - License Number: `30-8942-019` (Monospace, ~7px in reference / proposed 11px, `#FFFFFF`).
  - Class: `CLASS 4 / CLASS 3 - PRIVATE MOTOR` (Regular 400, ~6.5px in reference / proposed 10px, `#D5D8E9`).
  - Expiry: `EXP: 2034/12` (Monospace, ~6.5px in reference / proposed 10px, `#D5D8E9`).

### 7.2 Jordan Map HUD (`E06b`)
- **Visual Purpose:** National geographic sovereignty HUD; visually anchors the DVLD digital service platform to the sovereign territory of the Hashemite Kingdom of Jordan.
- **Hierarchy:** Visual extension of the holographic license HUD, situated directly adjacent in the upper-right sky dome.
- **Spatial Coordinates (Phase 01.1):**
  - Bounding Box: $X: 494\text{--}578\text{ px}$ ($48.24\%\text{--}56.45\%$, width $84\text{ px} / 8.20\%$), $Y: 36\text{--}140\text{ px}$ ($7.03\%\text{--}27.34\%$, height $104\text{ px} / 20.31\%$).
- **Color Treatment & Vector Treatment:**
  - Wireframe Outline: `color.brand.cyan.core` (`#6BDDFF`) 1.5px glowing polygonal stroke following the exact geopolitical border of Jordan.
  - Polygonal Fill: Ultra-translucent cyan wash (`rgba(107, 221, 255, 0.10)`).
  - Vertex Nodes: Circular glowing points (`#C7F1FF`) at boundary turns with subtle pulse animation.
  - Connection Vectors: Faint dashed cyan connection lines linking Amman Citadel, HQ, and territory nodes.

### 7.3 Institutional Footer Bar
- **Visual Purpose:** Statutory authority accreditation, legal copyright protection, and national vision branding.
- **Spatial Coordinates (Phase 01.1):**
  - Center Legal Notice: $X: 433\text{--}590\text{ px}$ ($42.29\%\text{--}57.62\%$, width $\sim 157\text{ px}$), $Y: 492\text{--}504\text{ px}$ ($96.09\%\text{--}98.44\%$, height $12\text{ px}$). Centered horizontally along bottom margin.
  - Right Vision Tagline: $X: 873\text{--}968\text{ px}$ ($85.25\%\text{--}94.53\%$, width $\sim 95\text{ px}$), $Y: 492\text{--}504\text{ px}$ ($96.09\%\text{--}98.44\%$, height $12\text{ px}$). Aligned right.
  - Viewport Clearance: $8\text{--}12\text{ px}$ from bottom edge in reference ($16\text{--}24\text{ px}$ proposed in 1080p).
- **Color Treatment & Typography (Proposed):**
  - Legal Copy: `color.text.secondary` (`#D5D8E9`) at 65% opacity. Text: "© 2026 DVLD. Authorized Personnel Only." (Regular 400, ~7px in reference / proposed 11px in 1080p).
  - Tagline Copy: `color.text.secondary` (`#D5D8E9`) with `color.brand.cyan.accent` (`#64C0F1`) indicator em-dash. Text: "Driving Jordan Forward —" (Regular 400, ~7px in reference / proposed 11px in 1080p).

### 7.4 Three Lower Mission Statements
- **Visual Purpose:** Departmental mission pillars communicating safety, digital efficiency, and long-term modernization.
- **Spatial Coordinates (Phase 01.1):**
  - Bounding Box: $X: 76\text{--}160\text{ px}$ ($7.42\%\text{--}15.63\%$, width $\sim 84\text{ px}$), $Y: 252\text{--}298\text{ px}$ ($49.22\%\text{--}58.20\%$, height $\sim 46\text{ px}$).
  - Clearance: Maintains a minimum $82\text{ px}$ horizontal clearance to the central illuminated portico ($X = 380\text{ px}$).
- **Color Treatment & Typography (Proposed):**
  - Line 1: "Safer Roads." (`color.text.secondary` `#D5D8E9`, Medium 500, ~8px in reference / proposed 12px in 1080p).
  - Line 2: "Smarter Services." (`color.text.secondary` `#D5D8E9`, Medium 500, ~8px in reference / proposed 12px in 1080p).
  - Line 3: "A Better Tomorrow." (`color.text.secondary` `#D5D8E9`, Medium 500, ~8px in reference / proposed 12px in 1080p).
  - Indicator Bar: `color.brand.cyan.accent` (`#64C0F1`) solid bar ($X: 76\text{--}100\text{ px}, Y: 308\text{ px}$, width $24\text{ px}$, height $2\text{ px}$).

### 7.5 Arabic Headquarters Canopy Signage
- **Visual Purpose:** Official national institutional identification mounted on the physical DVLD building architecture.
- **Spatial Coordinates (Phase 01.1):**
  - Position: Mounted on the dark fascia of the entrance canopy extending from the building entrance portal (`E03` / `E04`): $X: 508\text{--}628\text{ px}$ ($49.61\%\text{--}61.33\%$, width $\sim 120\text{ px}$), $Y: 294\text{--}312\text{ px}$ ($57.42\%\text{--}60.94\%$, height $\sim 18\text{ px}$).
  - Clearance to Login Card: Terminates at $X \approx 628\text{ px}$, maintaining a strict $14\text{ px}$ clearance ($1.37\%$ canvas width) to the login card's left outer rim ($X = 642\text{ px}$).
- **Content & Linguistic Direction:**
  - Signage Text: `دائرة ترخيص السواقين والمركبات` (Driving and Vehicle Licensing Department).
  - Reading Direction: Strictly Right-to-Left (RTL) with proper Arabic cursive ligatures.
- **Color Treatment & Lighting (Proposed):**
  - Backlit Channel Letterforms: `color.surface.card.rim` / `color.text.primary` (`#DFFAFF` / `#FFFFFF`).
  - Halo Radiance: Emissive cyan halo (`#6BDDFF`, opacity 0.5, blur 8px) casting glow onto dark canopy fascia.
  - Proposed Typography: Modern Architectural Kufic (`Cairo` Semi-bold 600 or `Tajawal` Semi-bold 600, ~18px reference / proposed ~32px in 1080p).

### 7.6 Left Branding Showcase (`E01`, `E02`)
- **Visual Purpose:** Institutional anchor, brand accreditation, and platform introduction.
- **Spatial Coordinates (Phase 01.1):** Left margin $X = 76\text{ px}$, top $Y = 52\text{ px}$, text width extends to $X = 336\text{ px}$.
- **Clearance:** Maintains $44\text{ px}$ horizontal clearance to HQ central portico ($X = 380\text{ px}$).

### 7.7 Right Login Authentication Card (`E10` – `E13`)
- **Visual Purpose:** Primary interactive authentication card for departmental personnel.
- **Dimensions & Radius:** $332 \times 414\text{ px}$ ($32.42\% \times 80.86\%$), proposed $r = 24\text{ px}$.
- **Surfaces:** Card body `rgba(0, 19, 49, 0.72)` with 20px blur and cyan rim; concentric Hashemite crown crest; dual credential inputs; primary action button; 3 SSO pills; verified token notice.

---

## 8. Motion Design Proposals (Accessible & Subtle)

> [!IMPORTANT]
> All motion parameters are **Proposed design specifications** and must strictly obey `@media (prefers-reduced-motion: reduce)`.

| Motion Element | Trigger | Duration (Proposed) | Easing (Proposed) | Visual Behavior | Reduced-Motion Fallback | Status |
|---|---|---|---|---|---|:---:|
| **Hologram Idle Hover** | Continuous ambient | 6.0s loop | `ease-in-out` (sine) | Card translates vertically $\pm 4\text{px}$; emitter cone pulses $\pm 5\%$ opacity | Static render at resting coordinate | `PROPOSED` |
| **Neon Curb Breathing** | Continuous ambient | 4.0s loop | `ease-in-out` | Neon tube glow fluctuates subtly between 95% and 105% luminance | Static continuous 100% emission | `PROPOSED` |
| **Celestial Star Twinkle** | Continuous ambient | 8.0s loop | Linear | Micro-twinkle on subtle background stars | Static background stars | `PROPOSED` |
| **Card Entrance Fade** | Page initial load | 600ms | `cubic-bezier(0.16, 1, 0.3, 1)` | Card fades from 0 to 1 opacity with slight upward rise (10px) | Instant render at 1.0 opacity | `PROPOSED` |
| **Button Hover State** | User pointer hover | 200ms | `ease-out` | Brightness increases +10%; drop shadow glow expands 4px | Instant color swap without transition | `PROPOSED` |
| **Input Focus Transition** | Input focus / active | 150ms | `ease-out` | Cyan border glow ring expands from 1px to 2px; caret illuminates | Instant border color change | `PROPOSED` |

---

## 9. Responsive Design Principles (Proposed Guidelines)

The five standard target viewports adapt according to the following design rules:
1. **1920 × 1080 (Primary Desktop Standard):** Baseline reference presentation (card width 420 px, display headline 36 px, body text 14 px). The wet road surface absorbs the 12.5% vertical frustum expansion.
2. **1600 × 900 (Standard Desktop):** Proportional scale (card width 380 px, headline 32 px). Preserves minimum 14 px clearance between building canopy and card border.
3. **1366 × 768 (Common Enterprise Laptop):** Card width 340 px, input height 42 px, headline 28 px. Left margin scales to 48 px.
4. **1280 × 720 (Compact Laptop):** Card width 320 px, input height 38 px, button height 40 px. Three pillars compact vertical spacing.
5. **1024 × 768 (Narrow Desktop / Tablet Landscape):** 3D scene camera dollies back slightly; card occupies 35% screen width. If width $< 960\text{ px}$, left branding collapses above login card to avoid collision.

---

## 10. Visual Accessibility & Contrast Verification

### 10.1 Mathematical Formula (WCAG 2.1 Specification)
Contrast ratios are computed using the exact W3C relative luminance ($L$) formula:
$$L = 0.2126 \cdot R + 0.7152 \cdot G + 0.0722 \cdot B$$
where normalized color channels $c \in [0, 1]$ are linearized:
$$c_{\text{linear}} = \begin{cases} \frac{c}{12.92} & \text{if } c \le 0.04045 \\ \left(\frac{c + 0.055}{1.055}\right)^{2.4} & \text{if } c > 0.04045 \end{cases}$$
The contrast ratio between two relative luminances $L_1 > L_2$ is:
$$\text{Ratio} = \frac{L_1 + 0.05}{L_2 + 0.05}$$

### 10.2 Relative Luminance Baseline
- Background Card Base (`#001331`): $L_{\text{bg}} = 0.006875$
- Primary White (`#FFFFFF`): $L_{\text{white}} = 1.000000$
- Secondary Slate (`#D5D8E9`): $L_{\text{slate}} = 0.665727$
- Electric Cyan (`#64C0F1`): $L_{\text{cyan}} = 0.504193$
- Muted Slate (`#94A3B8`): $L_{\text{muted}} = 0.368812$

### 10.3 Contrast Ratio Verification on Pure `#001331` Card Base

| Foreground Color Token | Foreground HEX | Background HEX | Relative Luminance (Fg) | Exact Contrast Ratio | WCAG AA Normal Text ($\ge 4.5:1$) | WCAG AA Large Text ($\ge 3:1$) | WCAG AAA Normal Text ($\ge 7:1$) | WCAG AAA Large Text ($\ge 4.5:1$) | Verified Result |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `color.text.primary` | `#FFFFFF` | `#001331` | $1.0000$ | **$18.46 : 1$** | **PASS** | **PASS** | **PASS** | **PASS** | **PASSES WCAG AAA** |
| `color.text.secondary` | `#D5D8E9` | `#001331` | $0.6657$ | **$13.04 : 1$** | **PASS** | **PASS** | **PASS** | **PASS** | **PASSES WCAG AAA** |
| `color.brand.cyan.accent` | `#64C0F1` | `#001331` | $0.5042$ | **$9.10 : 1$** | **PASS** | **PASS** | **PASS** | **PASS** | **PASSES WCAG AAA** |
| `color.text.muted` | `#94A3B8` | `#001331` | $0.3688$ | **$7.20 : 1$** | **PASS** | **PASS** | **PASS** | **PASS** | **PASSES WCAG AAA** |

### 10.4 Composite Context Analysis (Real-World Opacity & Background Interplay)
In actual implementation, the login card is not a 100% opaque solid, but a glassmorphic surface (`rgba(0, 19, 49, 0.72)` with a 20px backdrop blur). Depending on what passes behind the card, the effective background luminance changes:

1. **Card over Dark Nighttime Environment (`#010311`):**
   - Alpha-composited backdrop: $\text{rgb}(0, 15, 40)$, $L = 0.004928$.
   - `#FFFFFF`: **$19.11 : 1$** (Passes WCAG AAA)
   - `#D5D8E9`: **$13.49 : 1$** (Passes WCAG AAA)
   - `#64C0F1`: **$9.42 : 1$** (Passes WCAG AAA)
   - `#94A3B8`: **$7.45 : 1$** (Passes WCAG AAA)
2. **Card over High-Intensity Red Road Reflection (`#FC0F61` pass-through):**
   - Alpha-composited backdrop: $\text{rgb}(71, 18, 62)$, $L = 0.021200$.
   - `#FFFFFF`: **$14.75 : 1$** (Passes WCAG AAA)
   - `#D5D8E9`: **$10.41 : 1$** (Passes WCAG AAA)
   - `#64C0F1`: **$7.27 : 1$** (Passes WCAG AAA)
   - `#94A3B8`: **$5.75 : 1$** $\rightarrow$ **FAILS WCAG AAA Normal Text** (requires $\ge 7.0:1$), but **PASSES WCAG AA Normal Text** ($\ge 4.5:1$) and **PASSES WCAG AAA Large Text** ($\ge 4.5:1$).

> [!CAUTION]
> **Implementation Mandate:** Because high-luminance light streaks passing behind translucent glass reduce contrast, `color.text.muted` (`#94A3B8`) must NOT be used for critical body text or normal-sized required instructions. It is strictly restricted to secondary non-critical placeholders, or the card backdrop blur/fill opacity must be maintained at $\ge 0.75$ to guarantee compliance across all camera angles.

### 10.5 Accessibility Controls
- **Non-Color Error Status:** Form validation errors must combine red border highlighting (`#EF4444`) with an explicit alert icon and error text message.
- **Focus Rings:** Unambiguous 2px cyan focus ring (`#16B3FF`) with 2px offset on all interactive controls.
- **RTL Integrity:** Arabic canopy signage and any localized controls must preserve natural cursive glyph connectivity and right-to-left layout direction.
