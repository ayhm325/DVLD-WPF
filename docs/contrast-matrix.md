# DVLD Contrast Matrix (WCAG 2.2)

> Computed with the WCAG relative-luminance formula (`(L1 + 0.05) / (L2 + 0.05)`).
> Text threshold: **4.5:1** normal, **3:1** large/UI. Ratios are measured, not estimated.

## Light theme

| Foreground | Background | Ratio | Normal text | Large text / UI | Approved use |
|---|---|---:|---|---|---|
| `#202321` | `#F3F0E7` | **13.92** | PASS (AAA) | PASS | Body, headings |
| `#202321` | `#FFFFFF` | **15.86** | PASS (AAA) | PASS | Body on white |
| `#202321` | `#DCDDD5` | **11.59** | PASS (AAA) | PASS | Text on muted surfaces |
| `#62665F` | `#F3F0E7` | **5.14** | PASS (AA) | PASS | Secondary text |
| `#62665F` | `#FFFFFF` | **5.86** | PASS (AA) | PASS | Secondary text on login form surfaces |
| `#202321` | `#B7E52B` | **10.77** | PASS (AAA) | PASS | Charcoal text on lime buttons |
| `#B7E52B` | `#202321` | **10.77** | PASS (AAA) | PASS | Lime text on charcoal |
| `#B7E52B` | `#F3F0E7` | **1.29** | **FAIL** | **FAIL** | **Lime must never be text/icon on cream** |
| `#27834A` | `#FFFFFF` | **4.74** | PASS (AA) | PASS | Success text |
| `#C44F4F` | `#FFFFFF` | **4.59** | PASS (AA) | PASS | Error text |
| `#3979B8` | `#FFFFFF` | **4.56** | PASS (AA) | PASS | Info text |
| `#C58B25` | `#FFFFFF` | **2.96** | **FAIL** | **FAIL** | **Amber is background/border only** |
| `#A36A15` | `#FFFFFF` | **4.54** | PASS (AA) | PASS | Warning **text** (required variant) |
| `#D0D2CA` | `#202321` | **10.39** | PASS (AAA) | PASS | Login brand panel supporting text |
| `#C9CDC5` | `#202321` | **9.84** | PASS (AAA) | PASS | Login brand panel supporting text |

## Dark theme

| Foreground | Background | Ratio | Normal text | Large text / UI | Approved use |
|---|---|---:|---|---|---|
| `#F3F0E7` | `#171A18` | **15.39** | PASS (AAA) | PASS | Body, headings |
| `#F3F0E7` | `#202321` | **13.92** | PASS (AAA) | PASS | Text on charcoal surfaces |
| `#A8ABA5` | `#171A18` | **7.54** | PASS (AAA) | PASS | Secondary text |
| `#A8ABA5` | `#202321` | **6.82** | PASS (AA) | PASS | Secondary text on dark login surface |
| `#B7E52B` | `#171A18` | **11.91** | PASS (AAA) | PASS | Lime text/accent on dark |
| `#4FAE6E` | `#202321` | **5.75** | PASS (AA) | PASS | Success text |
| `#E07373` | `#202321` | **5.19** | PASS (AA) | PASS | Error text |
| `#E0A93D` | `#202321` | **7.49** | PASS (AAA) | PASS | Warning text |
| `#6BA5DB` | `#202321` | **6.07** | PASS (AA) | PASS | Info text |

## Corrections to the specification's draft matrix

The draft in the specification listed `#C58B25` on white as **3.9:1**. Measured value is **2.96:1**, which fails even the 3:1 large-text threshold. The mandated warning-text token is therefore `#A36A15` (**4.54:1**). `#F3F0E7`/`#202321` measures **13.92:1**, not 14.2:1; the conclusion (AAA) is unchanged.

## Enforcement rules

1. Acid Lime (`#B7E52B`) is **never** text or iconography on cream/white.
2. Warning text on light surfaces uses `#A36A15`; `#C58B25` is limited to backgrounds, borders and dark-theme text.
3. Every new pairing must be appended here with a measured ratio before use.
4. Focus ring (`#B7E52B`) against cream is 1.29:1, below the 3:1 UI threshold; the implementation must add a charcoal outline or a charcoal-backed offset so the focus indicator itself reaches ≥3:1.
