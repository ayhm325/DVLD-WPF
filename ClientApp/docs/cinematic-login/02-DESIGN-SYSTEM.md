# DVLD Cinematic Login — Visual Design System & Design Tokens

> **Document ID:** `02-DESIGN-SYSTEM`  
> **Phase:** 02.3.1 — Final Documentation Consistency Pass
> **Status:** AUDITED & EMPIRICALLY MEASURED — SPECIFICATION SPEC  
> **Authoritative For:** Color Palette, Value Origins, Typography Hierarchy, Spacing Scale, Authoritative Phase 01 Coordinates, 1080p Scaling Methodology, Glassmorphism, Lighting Language, Full Component Specifications, and WCAG Contrast Verification  
> **Visual Source of Truth:** `media_1793445589710_98905759.jpg` (SHA-256: `c94651c3aaf39d38f62824d1e1e784f2c1e65266c35ede15ef1be2b9579c67d0`)  
> **Spatial Baseline Authority:** `03-COMPOSITION-SPEC.md` (1024 × 512 pixels, 2.0 : 1 aspect ratio)

---

## 1. Design System Architecture & Token Taxonomy

The DVLD design system establishes a semantic design-token architecture connecting 2D interactive UI controls with the 3D cinematic nocturnal environment.

### 1.1 Classification of Values (Reference-derived vs Proposed)
To maintain engineering integrity, all values in this design system are explicitly separated into two categories:
1. **Reference-derived Values:** Values directly measured, sampled, or calculated from the approved reference raster image (`1024 × 512`) as documented in the authoritative `03-COMPOSITION-SPEC.md`. These represent verified visual facts of the reference artifact.
2. **Proposed Values:** Engineering and aesthetic starting points selected for implementation. These include:
   - Target viewport dimensions (1080p and responsive viewports).
   - Glassmorphic parameters (backdrop blur radius, saturation multipliers, border opacity, composite surface opacity).
   - Radius values (card, button, and input corner radii).
   - Shadow formulas (blur radius, spread, ambient drop-shadow offsets).
   - 3D lighting parameters (color temperatures in Kelvin, luminous intensities, falloff distances).
   - Motion and transition parameters (durations, easing curves, translation offsets).
   - Typography font families, line-heights, and letter spacings.
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
| 1 | `color.background.zenith` | `#00102C`<br>`rgb(0, 16, 44)` | Reference-derived | Sampled: Sky zenith ($[500, 520) \times [10, 25)\text{ px}$) | Deepest celestial upper sky dome | `SAMPLED` | `PROPOSED` | `HIGH` |
| 2 | `color.background.base` | `#010311`<br>`rgb(1, 3, 17)` | Reference-derived | Sampled: Upper mid sky ($[200, 220) \times [40, 55)\text{ px}$) | Nocturnal atmospheric dark base | `SAMPLED` | `PROPOSED` | `HIGH` |
| 3 | `color.background.horizon` | `#C7F1FF`<br>`rgb(199, 241, 255)` | Reference-derived | Sampled: Horizon glow above HQ roofline | Distant cyan atmospheric horizon haze & moonlight | `SAMPLED` | `PROPOSED` | `HIGH` |
| 4 | `color.brand.cyan.core` | `#6BDDFF`<br>`rgb(107, 221, 255)` | Reference-derived | Sampled: Left Chevron Logo ($[85, 95) \times [50, 65)\text{ px}$) | Primary interactive cyan, HUD lines & logo | `SAMPLED` | `PROPOSED` | `HIGH` |
| 5 | `color.brand.cyan.accent` | `#64C0F1`<br>`rgb(100, 192, 241)` | Reference-derived | Sampled: Slogan text ($[130, 160) \times [180, 190)\text{ px}$) | Headline cyan accent, pills & indicators | `SAMPLED` | `PROPOSED` | `HIGH` |
| 6 | `color.brand.cyan.neon` | `#16B3FF`<br>`rgb(22, 179, 255)` | Reference-derived | Sampled: Road curb strip ($x = 91\text{ px}, y = 408\text{ px}$) | Emissive curb strip, card glow & focus rings | `SAMPLED` | `PROPOSED` | `HIGH` |
| 7 | `color.brand.blue.action` | `#178BFC`<br>`rgb(23, 139, 252)` | Reference-derived | Sampled: Button left edge ($x = 680\text{ px}, y = 356\text{ px}$) | Primary "Sign in ->" button gradient start | `SAMPLED` | `PROPOSED` | `HIGH` |
| 8 | `color.brand.blue.deep` | `#1849FB`<br>`rgb(24, 73, 251)` | Reference-derived | Sampled: Button right mid ($x = 910\text{ px}, y = 356\text{ px}$) | Primary "Sign in ->" button gradient end | `SAMPLED` | `PROPOSED` | `HIGH` |
| 9 | `color.surface.card.base` | `#001331`<br>`rgb(0, 19, 49)` | Reference-derived | Sampled: Card interior ($[810, 830) \times [240, 260)\text{ px}$) | Login glass card dark backdrop base fill | `SAMPLED` | `PROPOSED` | `HIGH` |
| 10 | `color.surface.card.rim` | `#DFFAFF`<br>`rgb(223, 250, 255)` | Reference-derived | Sampled: Card left border ($x = 654\text{ px}, y \in [200, 220)\text{ px}$) | Glass card perimeter border highlight stroke | `SAMPLED` | `PROPOSED` | `HIGH` |
| 11 | `color.surface.card.glow` | `#3A47AD`<br>`rgb(58, 71, 173)` | Reference-derived | Sampled: Card outer margin ($[638, 648) \times [200, 220)\text{ px}$) | Glass card diffuse cyan-blue outer aura | `SAMPLED` | `PROPOSED` | `HIGH` |
| 12 | `color.surface.input.fill` | `#212D50`<br>`rgb(33, 45, 80)` | Reference-derived | Sampled: Username input field interior | Input field translucent background fill | `SAMPLED` | `PROPOSED` | `HIGH` |
| 13 | `color.surface.input.border` | `#13274A`<br>`rgb(19, 39, 74)` | Reference-derived | Sampled: Username input border edge | Input field subtle perimeter border stroke | `SAMPLED` | `PROPOSED` | `HIGH` |
| 14 | `color.surface.sso.fill` | `#0A203E`<br>`rgb(10, 32, 62)` | Reference-derived | Sampled: SSO button ($[700, 720) \times [415, 425)\text{ px}$) | Auxiliary SSO pill button surface fill | `SAMPLED` | `PROPOSED` | `HIGH` |
| 15 | `color.architectural.amber` | `#FD7E2F`<br>`rgb(253, 126, 47)` | Reference-derived | Sampled: Temple of Hercules ($x = 600\text{ px}, y = 176\text{ px}$) | Roman columns golden floodlight glow | `SAMPLED` | `PROPOSED` | `HIGH` |
| 16 | `color.architectural.warm` | `#FFF6E1`<br>`rgb(255, 246, 225)` | Reference-derived | Sampled: HQ lobby ($[360, 380) \times [295, 305)\text{ px}$) | Headquarters interior warm lobby lights | `SAMPLED` | `PROPOSED` | `HIGH` |
| 17 | `color.automotive.red` | `#D60927`<br>`rgb(214, 9, 39)` | Reference-derived | Sampled: Sedan taillight ($x = 327\text{ px}, y = 379\text{ px}$) | Sedan LED taillight core strip | `SAMPLED` | `PROPOSED` | `HIGH` |
| 18 | `color.automotive.refl` | `#FC0F61`<br>`rgb(252, 15, 97)` | Reference-derived | Sampled: Road reflection ($x = 252\text{ px}, y = 449\text{ px}$) | Wet road specular red reflection streak | `SAMPLED` | `PROPOSED` | `HIGH` |
| 19 | `color.text.primary` | `#FFFFFF`<br>`rgb(255, 255, 255)` | Reference-derived | Sampled: Headline ($[80, 100) \times [150, 160)\text{ px}$) | Primary titles, slogans & button labels | `SAMPLED` | `PROPOSED` | `HIGH` |
| 20 | `color.text.secondary` | `#D5D8E9`<br>`rgb(213, 216, 233)` | Reference-derived | Sampled: Description ($[100, 150) \times [215, 225)\text{ px}$) | Explanatory copy, subtitles & metadata | `SAMPLED` | `PROPOSED` | `HIGH` |
| 21 | `color.text.muted` | `#94A3B8`<br>`rgb(148, 163, 184)` | Proposed | Synthetic UI candidate (Neutral Slate) | Input placeholders & non-critical labels | `SYNTHETIC` | `PROPOSED` | `MEDIUM` |
| 22 | `color.status.success` | `#218259`<br>`rgb(33, 130, 89)` | Reference-derived | Sampled: Shield badge ($x = 723\text{ px}, y = 445\text{ px}$) | Security notice verified shield icon & badge | `SAMPLED` | `PROPOSED` | `HIGH` |
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
> Reference sizes in $1024 \times 512$ are **Reference-derived** (estimated visual cap heights). All Target 1080p sizes, font family candidates, line heights, and letter spacings are **Proposed**.

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
A mathematical discrepancy exists between the reference image and standard web viewports:
- **Reference Resolution:** $1024 \times 512\text{ px}$, Aspect Ratio = $2.0 : 1$ (panoramic ultrawide).
- **Target Standard Desktop:** $1920 \times 1080\text{ px}$, Aspect Ratio = $16:9 \approx 1.7778 : 1$.

```text
Horizontal scaling factor:  Sx = 1920 / 1024 = 1.875x
Vertical scaling factor:    Sy = 1080 / 512  = 2.109375x
Aspect Ratio Ratio (Sy/Sx): 2.109375 / 1.875 = 1.125  (+12.5% vertical height)
```

Because 16:9 is **12.5% taller** than 2.0:1, applying a single uniform multiplier globally is geometrically impossible:
- A uniform $1.875\times$ scale leaves $120\text{ px}$ of empty space vertically ($512 \times 1.875 = 960\text{ px} \ne 1080\text{ px}$).
- A uniform $2.109375\times$ scale produces $2160\text{ px}$ width ($1024 \times 2.109375 = 2160\text{ px}$), clipping $240\text{ px}$ horizontally.

### 4.2 Documented Scaling Methodologies
To preserve visual harmony without distortion, the design system utilizes 5 explicit scaling mechanisms:
1. **Percentage-based Horizontal Positioning:** Elements tied to layout anchors (e.g. left branding margin at $7.42\% \approx 140\text{ px}$ at 1920px width) scale relative to viewport width.
2. **Independent Width and Height Adaptation:** The 3D scene camera frustum ($FOV_y$) keeps the architectural skyline within the upper 60% of vertical space, while the foreground wet asphalt road plane expands downward to absorb the extra $12.5\%$ vertical height at 16:9.
3. **Fixed-Size UI Ergonomics Constraints:** Interactive targets are constrained by human motor ergonomics and WCAG touch targets (input height is capped at $46\text{ px}$; button height is fixed at $48\text{ px}$; card width is capped at $420\text{ px}$ to prevent overly long inputs).
4. **Uniform Scaling for Graphic Marks:** Vector marks (DVLD chevron logo, security crest, SSO icons) scale uniformly ($1:1$) to prevent distortion.
5. **Responsive Layout Adaptations:** Across breakpoints, margins and card widths contract gracefully to maintain a strict minimum clearance buffer of $14\text{ px}$ to the headquarters canopy.

### 4.3 Component Sizing & Authoritative Spatial Coordinates

> [!IMPORTANT]
> All reference coordinates in this table are audited against the authoritative `03-COMPOSITION-SPEC.md`.

| Parameter / Dimension | Authoritative Reference Measured (1024x512) | Value Origin | Proposed 1080p Equivalent | Scaling Method Used | Value Status |
|---|:---:|:---:|:---:|---|:---:|
| **Left Viewport Margin (`spacing.viewport.left`)** | `76 px` ($7.42\%$) | Reference-derived | `140 px` | Percentage ($7.42\% \times 1920 \approx 142\text{px}$) | `PROPOSED` |
| **Right Viewport Margin (`spacing.viewport.right`)** | `38 px` ($3.71\%$, $1024 - 986$) | Reference-derived | `71 px` | Percentage ($3.71\% \times 1920 \approx 71\text{px}$) | `PROPOSED` |
| **Top Viewport Margin (`spacing.viewport.top`)** | `52 px` ($10.16\%$) | Reference-derived | `96 px` | Independent Height ($10.16\% \times 1080 \approx 109\text{px}$) | `PROPOSED` |
| **Bottom Viewport Margin (`spacing.viewport.bottom`)** | `46 px` ($8.98\%$, $512 - 466$) | Reference-derived | `88 px` | Independent Height ($8.98\% \times 1080 \approx 97\text{px}$) | `PROPOSED` |
| **Login Card Frame (`E10`) Bounds** | `[654, 986) × [52, 466) px`<br>($W: 332\text{ px}, H: 414\text{ px}$) | Reference-derived | `420 × 640 px` | Ergonomic UI constraint (capped at 420px) | `PROPOSED` |
| **Login Card Outer Halo Bounds** | `[630, 1010) × [32, 486) px`<br>($W: 380\text{ px}, H: 454\text{ px}$) | Reference-derived | `470 × 690 px` | Visual diffusion aura | `PROPOSED` |
| **Holographic License Card (`E06a`)** | `[322, 480) × [40, 198) px`<br>($W: 158\text{ px}, H: 158\text{ px}$) | Reference-derived | `240 × 240 px` | Scaled 3D HUD bounding frame | `PROPOSED` |
| **Holographic Emitter Cone (`E06b`)** | `[358, 446) × [160, 226) px`<br>($W: 88\text{ px}, H: 66\text{ px}$) | Reference-derived | `140 × 100 px` | 3D projection cone | `PROPOSED` |
| **Wireframe Jordan Map HUD (`E06c`)** | `[490, 570) × [35, 140) px`<br>($W: 80\text{ px}, H: 105\text{ px}$) | Reference-derived | `130 × 170 px` | Vector geographic HUD | `PROPOSED` |
| **Three Lower Mission Statements (`E02b`)** | `[76, 190) × [430, 475) px`<br>($W: 114\text{ px}, H: 45\text{ px}$) | Reference-derived | `180 × 70 px` | Lower-left margin alignment | `PROPOSED` |
| **Arabic Canopy Signage (`E04b`)** | `[495, 625) × [285, 330) px`<br>($W: 130\text{ px}, H: 45\text{ px}$) | Reference-derived | `210 × 72 px` | Architectural canopy fascia | `PROPOSED` |
| **Input Field Dimensions (`size.input`)** | `286 × 30 px` | Reference-derived | `356 × 46 px` | Fixed ergonomic touch target | `PROPOSED` |
| **Input-to-Input Gap (`spacing.form.gap`)** | `12 px` | Reference-derived | `16 px` | 4px Grid scale (`spacing.4`) | `PROPOSED` |
| **Sign-In Button Height (`size.button.height`)** | `32 px` | Reference-derived | `48 px` | Standard ergonomic button target | `PROPOSED` |
| **SSO Button Dimensions & Gap** | `88 × 30 px`, Gap `11 px` | Reference-derived | `108 × 44 px`, Gap `16 px` | Ergonomic touch target & 4px grid | `PROPOSED` |
| **Card-to-Building Canopy Clearance** | `14 px` ($1.37\%$, $654 - 640$) | Reference-derived | `26 px` | Percentage buffer ($1.37\% \times 1920 \approx 26\text{px}$) | `PROPOSED` |
| **Text-to-Portico Clearance** | `44 px` ($4.30\%$, $380 - 336$) | Reference-derived | `82 px` | Percentage buffer ($4.30\% \times 1920 \approx 82\text{px}$) | `PROPOSED` |

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

## 7. Complete Component Coverage & Authoritative Spatial Specifications

### 7.1 Floating Holographic Jordanian Driving License (`E06a`, `E06b`)
- **Visual Purpose:** Represents smart digital governance, modernized citizen credentials, and official authority floating in 3D space above the department headquarters.
- **Hierarchy:** Secondary focal anchor after the primary login card, positioned in the upper-mid architectural sky.
- **Authoritative Spatial Coordinates (`03-COMPOSITION-SPEC.md`):**
  - **Holographic Card Frame (`E06a`):** Canonical Bounds: `[322, 480) × [40, 198) px` ($W: 158\text{ px} / 15.43\%$, $H: 158\text{ px} / 30.86\%$; normalized $[0.3145, 0.4688) \times [0.0781, 0.3867)$). Center: $(401\text{ px}, 119\text{ px})$.
  - **Conical Projection Emitter Cone (`E06b`):** Canonical Bounds: `[358, 446) × [160, 226) px` ($W: 88\text{ px} / 8.59\%$, $H: 66\text{ px} / 12.89\%$; normalized $[0.3496, 0.4355) \times [0.3125, 0.4414)$).
  - 3D Orientation: Pitched backward $\sim -12^\circ$ with subtle counter-clockwise yaw ($\sim +15^\circ$).
- **Color Treatment:**
  - Card Perimeter Rim: `color.brand.cyan.core` (`#6BDDFF`) glowing border stroke with cyan halo.
  - Card Fill Plane: Translucent cyan-tinted glass (`rgba(0, 19, 49, 0.40)`).
  - Verified Status Badge: `color.status.success` (`#218259`) background pill with white checkmark and "VERIFIED" label.
  - Avatar Silhouette: Semi-transparent cyan user silhouette cutout (Bounds: `[330, 365) × [105, 155) px`).
- **Typography & Data Fields (Proposed):**
  - Title: "DRIVING LICENSE" (Bold 700, ~9px in reference / proposed 14px in 1080p, `#6BDDFF`).
  - License Number: `30-8942-019` (Monospace, ~7px in reference / proposed 11px, `#FFFFFF`).
  - Class: `CLASS 4 / CLASS 3 - PRIVATE MOTOR` (Regular 400, ~6.5px in reference / proposed 10px, `#D5D8E9`).
  - Expiry: `EXP: 2034/12` (Monospace, ~6.5px in reference / proposed 10px, `#D5D8E9`).

### 7.2 Jordan Map HUD (`E06c`)
- **Visual Purpose:** National geographic sovereignty HUD; visually anchors the DVLD digital service platform to the sovereign territory of the Hashemite Kingdom of Jordan.
- **Hierarchy:** Visual extension of the holographic license HUD, situated directly adjacent in the upper-right sky dome.
- **Authoritative Spatial Coordinates (`03-COMPOSITION-SPEC.md`):**
  - **Bounding Box (`E06c`):** Canonical Bounds: `[490, 570) × [35, 140) px` ($W: 80\text{ px} / 7.81\%$, $H: 105\text{ px} / 20.51\%$; normalized $[0.4785, 0.5566) \times [0.0684, 0.2734)$).
  - Overlap with Citadel Columns: Shares a 2D bounding region with the Roman columns (Canonical Overlap: `[520, 570) × [85, 140) px`) as an intentional semi-transparent overlay.
- **Color Treatment & Vector Treatment:**
  - Wireframe Outline: `color.brand.cyan.core` (`#6BDDFF`) 1.5px glowing polygonal stroke following the exact geopolitical border of Jordan.
  - Polygonal Fill: Ultra-translucent cyan wash (`rgba(107, 221, 255, 0.10)`).
  - Vertex Nodes: Circular glowing points (`#C7F1FF`) at boundary turns with subtle pulse animation.
  - Connection Vectors: Faint dashed cyan connection lines linking Amman Citadel, HQ, and territory nodes.

### 7.3 Institutional Footer Bar
- **Visual Purpose:** Statutory authority accreditation, legal copyright protection, and national vision branding.
- **Spatial Coordinates (`03-COMPOSITION-SPEC.md` Baseline):**
  - Center Legal Notice: Canonical Bounds: `[433, 590) × [492, 504) px` ($W: 157\text{ px}$, $H: 12\text{ px}$; normalized $[0.4229, 0.5762) \times [0.9609, 0.9844)$). Centered horizontally along bottom margin.
  - Right Vision Tagline: Canonical Bounds: `[873, 968) × [492, 504) px` ($W: 95\text{ px}$, $H: 12\text{ px}$; normalized $[0.8525, 0.9453) \times [0.9609, 0.9844)$). Aligned right.
  - Viewport Clearance: $8\text{ to }12\text{ px}$ from bottom edge in reference ($16\text{ to }24\text{ px}$ proposed in 1080p).
- **Color Treatment & Typography (Proposed):**
  - Legal Copy: `color.text.secondary` (`#D5D8E9`) at 65% opacity. Text: "© 2026 DVLD. Authorized Personnel Only." (Regular 400, ~7px in reference / proposed 11px in 1080p).
  - Tagline Copy: `color.text.secondary` (`#D5D8E9`) with `color.brand.cyan.accent` (`#64C0F1`) indicator em-dash. Text: "Driving Jordan Forward —" (Regular 400, ~7px in reference / proposed 11px in 1080p).

### 7.4 Three Lower Mission Statements (`E02b`)
- **Visual Purpose:** Departmental mission pillars communicating safety, digital efficiency, and long-term modernization.
- **Authoritative Spatial Coordinates (`03-COMPOSITION-SPEC.md`):**
  - **Bounding Box (`E02b`):** Canonical Bounds: `[76, 190) × [430, 475) px` ($W: 114\text{ px} / 11.13\%$, $H: 45\text{ px} / 8.79\%$; normalized $[0.0742, 0.1855) \times [0.8398, 0.9277)$).
  - Spatial Positioning Note: In the reference image, the three pillars sit down in the lower-left corner alongside the wet roadway plane ($Y \in [360, 512)\text{ px}$) and to the left of the luxury sedan ($x_{\text{min}} = 188\text{ px}$), separated from the upper description text ($Y \in [210, 248)\text{ px}$) by a broad vertical clearance of $182\text{ px}$ ($430 - 248 = 182\text{ px}$, interval $[248, 430)\text{ px}$).
- **Color Treatment & Typography (Proposed):**
  - Line 1: "Safer Roads." (`color.text.secondary` `#D5D8E9`, Medium 500, ~8px in reference / proposed 12px in 1080p).
  - Line 2: "Smarter Services." (`color.text.secondary` `#D5D8E9`, Medium 500, ~8px in reference / proposed 12px in 1080p).
  - Line 3: "A Better Tomorrow." (`color.text.secondary` `#D5D8E9`, Medium 500, ~8px in reference / proposed 12px in 1080p).
  - Indicator Bar: `color.brand.cyan.accent` (`#64C0F1`) solid bar (Bounds: `[76, 100) × [473, 475) px`, $W: 24\text{ px}$, $H: 2\text{ px}$).

### 7.5 Arabic Headquarters Canopy Signage (`E04b`)
- **Visual Purpose:** Official national institutional identification mounted on the physical DVLD building architecture.
- **Authoritative Spatial Coordinates (`03-COMPOSITION-SPEC.md`):**
  - **Bounding Box (`E04b`):** Canonical Bounds: `[495, 625) × [285, 330) px` ($W: 130\text{ px} / 12.70\%$, $H: 45\text{ px} / 8.79\%$; normalized $[0.4834, 0.6104) \times [0.5566, 0.6445)$).
  - Clearance to Login Card: The canopy terminates at $x_{\text{max}} \approx 640\text{ px}$, maintaining a strict $14\text{ px}$ clearance ($1.37\%$ canvas width) to the solid login card border ($x_{\text{min}} = 654\text{ px}$, clearance interval $[640, 654)\text{ px}$).
- **Content & Linguistic Direction:**
  - Signage Text: `دائرة ترخيص السواقين والمركبات` (Driving and Vehicle Licensing Department).
  - Reading Direction: Strictly Right-to-Left (RTL) with proper Arabic cursive ligatures.
- **Color Treatment & Lighting (Proposed):**
  - Backlit Channel Letterforms: `color.surface.card.rim` / `color.text.primary` (`#DFFAFF` / `#FFFFFF`).
  - Halo Radiance: Emissive cyan halo (`#6BDDFF`, opacity 0.5, blur 8px) casting glow onto dark canopy fascia.
  - Proposed Typography: Modern Architectural Kufic (`Cairo` Semi-bold 600 or `Tajawal` Semi-bold 600, ~18px reference / proposed ~32px in 1080p).

### 7.6 Left Branding Showcase (`E01`, `E02`)
- **Visual Purpose:** Institutional anchor, brand accreditation, and platform introduction.
- **Authoritative Coordinates (`03-COMPOSITION-SPEC.md`):**
  - Header Group (`E01`): Canonical Bounds: `[76, 300) × [43, 118) px` ($W: 224\text{ px}, H: 75\text{ px}$).
  - Slogan & Description (`E02`): Canonical Bounds: `[76, 336) × [134, 248) px` ($W: 260\text{ px}, H: 114\text{ px}$).
- **Clearance:** Description terminates at $x_{\text{max}} = 336\text{ px}$, maintaining $44\text{ px}$ horizontal clearance to the bright entrance portal ($x_{\text{min}} = 380\text{ px}$, clearance interval $[336, 380)\text{ px}$).

### 7.7 Right Login Authentication Card (`E10` – `E13`)
- **Visual Purpose:** Primary interactive authentication card for departmental personnel.
- **Authoritative Coordinates (`03-COMPOSITION-SPEC.md`):**
  - Solid Glass Frame (`E10`): Canonical Bounds: `[654, 986) × [52, 466) px` ($W: 332\text{ px}, H: 414\text{ px}$).
  - Outer Diffuse Cyan Halo: Canonical Bounds: `[630, 1010) × [32, 486) px` ($W: 380\text{ px}, H: 454\text{ px}$).
- **Proposed CSS Starting Values:** Card body `rgba(0, 19, 49, 0.72)` with 20px blur and cyan rim; concentric Hashemite crown crest (`E11`); dual credential inputs (`E12`); primary action button (`E12`); 3 SSO pills (`E13`); verified token notice (`E13`).

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
$$L = 0.2126 \cdot R_{\text{linear}} + 0.7152 \cdot G_{\text{linear}} + 0.0722 \cdot B_{\text{linear}}$$
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

### 10.4 Proposed Glassmorphism Values & Mandatory Readability Testing

> [!IMPORTANT]
> **Classification Notice:** Proposed parameters such as `card opacity >= 0.72` and `backdrop blur >= 20px` are **PROPOSED IMPLEMENTATION STARTING VALUES**, NOT mathematically verified universal accessibility guarantees.
> 
> **Optical Reality:** Backdrop blur diffuses high spatial frequencies (sharp edges), but does **NOT** eliminate low-frequency background luminance. When high-intensity scene elements (e.g. wet road specular streaks `#FC0F61` or bright architectural headlights) pass behind a semi-transparent surface, the transmitted luminous energy raises the composite background luminance, directly degrading text contrast.
> 
> **Testing Mandate:** During implementation (Phases 06 and 07), actual text contrast must be empirically tested and measured against dynamic, representative 3D backgrounds across multiple camera angles. If specular streaks beneath the card reduce contrast below WCAG thresholds, fill opacity must be increased dynamically or a dark inner backing scrim must be applied.

- **Non-Color Error Status:** Form validation errors must combine red border highlighting (`#EF4444`) with an explicit alert icon and error text message.
- **Focus Rings:** Unambiguous 2px cyan focus ring (`#16B3FF`) with 2px offset on all interactive controls.
- **RTL Integrity:** Arabic canopy signage and any localized controls must preserve natural cursive glyph connectivity and right-to-left layout direction.
