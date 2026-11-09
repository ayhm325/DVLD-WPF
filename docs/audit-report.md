# DVLD Frontend Audit Report

**Phase 0 deliverable for the ULTIMATE FRONTEND TRANSFORMATION specification.**
**Date:** 2026-11-09
**Baseline commit:** `04147a36f24771cf92162e6c86a23bbe881cdbed`
**Scope inspected:** `ClientApp/` only. At the start of this verification, this report, `docs/contrast-matrix.md`, and the three login screenshots were already present as untracked files. They were preserved; only this report was amended with current build and visual evidence.

---

## Executive Summary

The DVLD SPA is a mature, feature-complete React 19 application: 27 routed pages, 15 service modules, role-based navigation, lazy-loaded route groups, an in-memory access token with refresh-cookie bootstrap (AUTH-01), and a custom fetch client with single-flight refresh. Functionally it is organized and cohesive.

Visually it is the **opposite of the target identity**: a dark-only navy/sapphire interface with cyan glow accents, 16–20px radii, glassmorphism, and heavy gradient/shadow decoration. There is **no light theme, no theme provider, no toggle, no serif/editorial typography, and no token layer matching the mandated lime/cream/charcoal palette**. Styling spans 7 CSS files and 4,321 physical lines (`App.css` 2,672; `Login.css` 802; `index.css` 502; the other four stylesheets 345 combined), which makes a system-level restyle the central risk.

The build succeeds; 74 JS chunks are produced (route-level lazy loading is already present), but the entry chunk is **501.78 KB raw / 157.0 KB gzip** and Vite warns about it. No unit/E2E/a11y test framework exists, so "zero functional regression" cannot be machine-verified without adding tooling (which requires explicit approval under §19).

**Recommendation:** proceed with the phased plan, starting with tokens/theme, then primitives/composites, then shell/pages. Treat the current blue identity as fully replaced, not layered.

---

## Architecture Summary

- **Feature-based composition** under `src/components/<feature>/`, shared UI under `components/ui` and `components/common`, layout under `components/layout`.
- **Routing:** no React Router. A `NavigationContext` holds `activePage`, and `AppRouter` resolves page names through `router/routeConfig.ts` to seven lazy route groups (`React.lazy` + `Suspense`).
- **State:** React Context only (`AuthContext`, `NavigationContext`, `PeopleContext`) plus local hooks. No state library.
- **API:** custom `apiClient.ts` (`fetch` wrapper, ProblemDetails parsing, single-flight refresh, one retry, `credentials: 'include'`); 15 feature services.
- **Auth:** access token in memory (`authTokenStore`), refresh via HttpOnly cookie, startup restore, cross-tab `BroadcastChannel` logout. Protected rendering is `App.tsx` + `AppRouter` + `permissions.ts`.
- **Permissions:** explicit `PAGE_PERMISSIONS` map for 27 pages (`Admin` / `Staff`), mirrored from backend policies.
- **Build:** Vite 8 (Rolldown), TypeScript 6 (`erasableSyntaxOnly`, `verbatimModuleSyntax`, `noUnused*`), Tailwind CSS v4 via `@tailwindcss/vite` and `@theme` in `index.css`. Path alias `@/*`.

---

## Dependencies

| Dependency | Version | Purpose | Used where | Keep/Replace/Remove |
|---|---|---|---|---|
| react | ^19.3.1 | UI runtime | `src/` | Keep |
| react-dom | ^19.3.1 | DOM renderer | `src/main.tsx` | Keep |
| @radix-ui/react-checkbox | ^1.4.1 | Checkbox primitive | No import found under `src/` | Review/remove candidate; no current UI use found |
| @radix-ui/react-dialog | ^1.3.1 | Dialog primitive | `src/components/ui/dialog.tsx`, modal composites | Keep |
| @radix-ui/react-dropdown-menu | ^2.2.1 | Accessible menu primitive | `src/components/ui/dropdown-menu.tsx` | Keep |
| @radix-ui/react-label | ^2.1.17 | Accessible form labels | `src/components/ui/label.tsx` | Keep |
| @radix-ui/react-select | ^2.4.1 | Accessible select primitive | `src/components/ui/select.tsx` | Keep |
| @radix-ui/react-separator | ^1.2.0 | Separator primitive | No import found under `src/` | Review/remove candidate; no current UI use found |
| @radix-ui/react-slot | ^1.5.0 | Polymorphic component slot | `src/components/ui/button.tsx` | Keep |
| @radix-ui/react-switch | ^1.4.1 | Switch primitive | No import found under `src/` | Review/remove candidate; no current UI use found |
| @radix-ui/react-tooltip | ^1.4.1 | Tooltip primitive | No import found under `src/` | Review/remove candidate; no current UI use found |
| @tailwindcss/vite | ^4.3.3 | Tailwind v4 Vite integration | `vite.config.ts` | Keep |
| class-variance-authority | ^0.7.1 | Typed component variants | `src/components/ui/button.tsx`, `badge.tsx` | Keep |
| clsx | ^2.1.1 | Conditional class composition | `src/lib/utils.ts` | Keep |
| lucide-react | ^1.65.0 | Icon set | UI and page components | Keep |
| motion | ^14.9.0 | UI transitions | `src/components/layout/Sidebar.tsx` | Keep; verify reduced motion during implementation |
| tailwind-merge | ^3.7.0 | Conflict-aware Tailwind classes | `src/lib/utils.ts` | Keep |
| tailwindcss | ^4.3.3 | Utility CSS and `@theme` | `src/index.css` | Keep |
| @eslint/js | ^10.0.1 | ESLint base configuration | `eslint.config.js` | Keep (development) |
| @types/node | ^24.19.2 | Node type definitions | Vite config | Keep (development) |
| @types/react | ^19.3.0 | React type definitions | TypeScript build | Keep (development) |
| @types/react-dom | ^19.3.0 | React DOM type definitions | TypeScript build | Keep (development) |
| @vitejs/plugin-react | ^6.1.2 | React transform and Fast Refresh | `vite.config.ts` | Keep (development) |
| eslint | ^10.13.0 | Static linting | `npm run lint` | Keep (development) |
| eslint-plugin-react-hooks | ^7.1.1 | React Hooks lint rules | `eslint.config.js` | Keep (development) |
| eslint-plugin-react-refresh | ^0.5.7 | Fast Refresh lint rules | `eslint.config.js` | Keep (development) |
| globals | ^17.13.0 | ESLint environment globals | `eslint.config.js` | Keep (development) |
| typescript | ~6.0.3 | Type checking | `npm run build` | Keep (development) |
| typescript-eslint | ^8.71.2 | TypeScript ESLint integration | `eslint.config.js` | Keep (development) |
| vite | ^8.3.4 | Dev server and production bundler | `npm run dev`, `build`, `preview` | Keep (development) |

Not present: React Router, chart library, form library, state library, UI kit, i18n framework, Vitest/Jest, Playwright, axe, Lighthouse. **Do not add alternatives** except where §19 explicitly pre-approves (axe dev-only, virtualization only if measured).

---

## Current Design Issues

| # | Severity | Issue | Evidence |
|---|---|---|---|
| D1 | **High** | No light theme at all. No `data-theme`, `ThemeProvider`, toggle, or `prefers-color-scheme` handling. | `index.css` defines one dark palette on `:root`; no theme provider/API found in `src/`. |
| D2 | **High** | Palette is blue/sapphire/cyan, not the mandated lime/cream/charcoal. | `index.css:86-124` and cyan text/glows in `Sidebar.tsx`/`Login.css`. |
| D3 | **High** | Styling is spread across global and feature files with duplicated surfaces/glows and no semantic token layer for the target palette. | 7 CSS files, 4,321 physical lines; `App.css` 2,672, `Login.css` 802, `index.css` 502, other stylesheets 345 combined. |
| D4 | **High** | Decoration exceeds the discipline rules: multiple radial glows, neon shadows, glass blur 16px, 16–20px radii everywhere. | `Login.css` glow orbs 420–460px, `blur(100px+)`; `border-radius: 1.25rem`; neon `box-shadow`. |
| D5 | **High** | Typography has no editorial layer: system sans only; Inter import is commented out; no serif. | `index.css:1-2`, `--sans` system stack, `--heading: var(--sans)`. |
| D6 | **Medium** | No chart library and no charts; dashboard is stat cards + tables. Spec's chart rules are currently inapplicable unless charts are added (out of scope). | `DashboardPage.tsx` imports no chart package. |
| D7 | **Medium** | Inconsistent sizing scale: radii 6/10/14/20 px plus ad-hoc `rounded-*`; spacing tokens exist but are mixed with arbitrary values. | `index.css` `@theme`, `App.css` arbitrary values. |
| D8 | **Low** | `components.json` (shadcn config) exists but shadcn is not a dependency; components are vendored. Harmless, but the config is misleading. | `ClientApp/components.json`. |
| D9 | **Low** | Documentation drift: both `ClientApp/AGENTS.md` and `ClientApp/README.md` describe access-token storage in `sessionStorage`; current code keeps the access token in memory and uses an HttpOnly refresh cookie. | `ClientApp/AGENTS.md`, `ClientApp/README.md` vs `src/utils/authTokenStore.ts` and `src/context/AuthContext.tsx`. |
| D10 | **Low** | Four direct Radix runtime dependencies appear unused by the current frontend source, increasing dependency surface and install footprint. | `@radix-ui/react-checkbox`, `react-separator`, `react-switch`, and `react-tooltip` are declared in `package.json` but no import was found under `src/`; verify no generated tooling relies on them before removing. |

---

## Responsive Issues

| # | Severity | Issue | Evidence |
|---|---|---|---|
| R1 | **High** | Only five breakpoint families exist (`639`, `767`, `1023`, `1024`); no 320-specific, 1280, or 1536 rules. | CSS media-query inventory. |
| R2 | **High — visually confirmed in the saved capture** | At 390×844, the login heading, explanatory text, inputs, remember/forgot-password row, submit button, and footer content extend beyond the right edge and are clipped. The page is not usable at this captured mobile width without horizontal access. | `docs/baseline-screenshots/login-390x844.png` (390×844 image inspected); `Login.css:776-800` applies mobile padding but does not prevent the captured content clipping. |
| R3 | **Medium** | Tables use fixed `min-width` 780px/280px, so mobile requires horizontal scroll (acceptable only inside the designated container). | `App.css:2555`, `App.css:2599`. |
| R4 | **Medium** | Sidebar has no desktop collapse state (260px fixed), contrary to the required expanded/collapsed model. | `--dvld-sidebar-width: 260px`; no collapsed-width rules. |
| R5 | **Low** | Touch targets: many icon buttons are 32–34px (`size-8.5`), below the 44px mobile requirement. | `Sidebar.tsx` footer/logout buttons. |

---

## Performance Concerns

**Measured baseline (fresh production build):**

| Metric | Value |
|---|---|
| Total JS raw | 1,119.1 KB |
| Total JS gzip | **336.4 KB** |
| JS chunks | **74** |
| Largest chunk | `index-BB0NMbEW.js` — 501.78 KB raw / **157.0 KB gzip** |
| CSS raw | **235.9 KB** |
| CSS gzip | **36.7 KB** |
| Build warning | chunk > 500 KB |
| Build exit | **0** (`npm.cmd --prefix ClientApp run build`, 2026-11-09) |

- Route-level lazy loading already exists (74 chunks), so the largest chunk is likely shared vendor/runtime, not one page.
- No Lighthouse, LCP, CLS, or console-error measurement was possible: Lighthouse/Playwright are not installed and no authenticated runtime could be established (no backend/test credentials). **These remain unmeasured, not assumed.**
- The CSS payload is 36.7 KB gzip. The build emitted the existing >500 KB JavaScript chunk warning; do not raise the warning threshold as an optimization. The CSS source is fragmented, but unused selectors have not been measured and are not asserted as a confirmed defect.

---

## Accessibility Issues

| # | Severity | Issue | Evidence |
|---|---|---|---|
| A1 | **High** | No skip-to-content link. | Zero `skip` matches. |
| A2 | **High** | `outline: none` used in 5 places (some with alternate focus, some without). Needs per-case review. | `Login.css:511`, `App.css:1108`, feature CSS. |
| A3 | **Medium** | `aria-sort` exists in only one table component; custom `<table>` usages may not expose sorting state. | 1 match; 17 `<table>` occurrences. |
| A4 | **Medium** | `aria-live` only in 2 places (spinner, access denied); filter/result-count announcements are missing. | 2 matches. |
| A5 | **Medium** | Dialog semantics: Radix dialog has proper behavior; custom `ModalShell`/`ConfirmModal` need a focus-trap/`aria-modal` audit. | `aria-modal` only in `CommandPalette`. |
| A6 | **Low** | Icon-only buttons: many have `aria-label`, some decorative icons lack `aria-hidden`. Needs a sweep. | Sidebar/Topbar icon usage. |
| A7 | Not verified | Contrast of the **current** palette across all surfaces was not measured; the target-palette matrix is complete (`docs/contrast-matrix.md`). | — |

---

## Functional Baseline (must preserve)

- [x] Login → refresh-cookie bootstrap → in-memory access token; logout revokes the session.
- [x] Route groups and page names resolved through `routeConfig.ts`; active page held in `NavigationContext`.
- [x] `PAGE_PERMISSIONS` mapping for 27 pages; `isPagePermitted` gates rendering; `AccessDenied` for denied pages.
- [x] Sidebar/Topbar/CommandPalette navigation and role-filtered visibility.
- [x] Single-flight refresh + one retry in `apiClient`; 401 clears state and redirects to login.
- [x] Server-driven pagination/filtering for People and other list pages (service parameters `pageNumber`/`pageSize`); preserve exactly.
- [x] Confirmation dialogs for logout and destructive actions.
- [x] Existing validation rules and error/ProblemDetails rendering.
- [x] Lazy route loading with a shared Suspense spinner.

**Do not change:** API URLs/payloads, auth/session logic, permission evaluation, route page names, validation rules, server-side pagination semantics.

---

## Page Inventory (27 pages)

| Route (page name) | Purpose | Roles | Table | Form | Modal | Data source | Complexity |
|---|---|---|---|---|---|---|---|
| Dashboard | KPIs + people overview | Admin, Staff | Yes | No | Confirm | dashboardService, peopleService | High |
| User Management | Users CRUD | Admin | Yes | Modals | Add/Edit/Details | userService | High |
| People | People list | Admin, Staff | Yes | No | Confirm | peopleService | High |
| Add Person | Create person | Admin, Staff | No | Yes | No | peopleService | High |
| Edit Person | Update person | Admin, Staff | No | Yes | No | peopleService | High |
| Person Details | Profile + history | Admin, Staff | Yes | No | Details | peopleService, licenseService | High |
| Drivers | Drivers list | Staff | Yes | No | No | driverService | Medium |
| Licenses | Issued licenses | Staff | Yes | No | Details | licenseService | High |
| Person License History | History table | Admin, Staff | Yes | No | Details | licenseService | Medium |
| International Licenses | List + issue | Staff | Yes | Yes | No | internationalLicenseService | High |
| Local Applications | Applications list | Staff | Yes | Modals | Details/Issue | localDrivingLicenseApplicationService | High |
| Test Appointments | Appointments list | Staff | Yes | No | Cancel | testAppointmentService | High |
| Manage Test Appointments | Manage workflow | Staff | Yes | Yes | Details | testAppointmentService | High |
| Written Test Appointments | Written test list | Staff | Yes | No | No | testAppointmentService | Medium |
| Appointment Details | Detail + actions | Staff | No | Yes | Confirm | testAppointmentService | High |
| Schedule Appointment | Booking form | Staff | No | Yes | No | testAppointmentService | High |
| Take Examination | Result form | Staff | No | Yes | No | testService/testAppointmentService | High |
| Renew License | Renewal form | Staff | No | Yes | No | licenseService | High |
| Replace License | Replacement form | Staff | No | Yes | No | licenseService | High |
| Detain License | Detention form | Staff | No | Yes | No | detainedLicenseService | High |
| Detained Licenses | Detained list | Staff | Yes | No | Release | detainedLicenseService | High |
| Application Types | Reference CRUD | Admin, Staff (edit Admin) | Yes | Inline | Edit | applicationTypeService | Medium |
| License Classes | Reference CRUD | Admin, Staff (edit Admin) | Yes | Inline | Edit | licenseClassService | Medium |
| Test Types | Reference CRUD | Admin, Staff (edit Admin) | Yes | Inline | Edit | testTypeService | Medium |
| Countries | Read-only reference | Admin, Staff | Yes | No | No | countryService | Low |
| My Profile | Profile + photo | Admin, Staff | No | Yes | No | userService/peopleService | High |
| Change Password | Password change | Admin, Staff | No | Yes | No | authService | Medium |

There are 27 protected route pages; Login is a separate unauthenticated screen. With Login included in the redesign inventory there are 28 user-facing screens, all in scope. Per the acknowledged decision packet, the revised rollout has 16 Tier-1 screens (Login plus 15 high-frequency protected pages) and 12 Tier-2 pages. The packet's stated 16/11 total is arithmetically inconsistent with its enumerated list; the listed Tier-2 pages are Drivers, International Licenses, Manage Test Appointments, Written Test Appointments, Appointment Details, Schedule Appointment, Detained Licenses, Application Types, License Classes, Test Types, Countries, and Change Password (12). The 16/12 split is used below; no page is deferred.

---

## Baseline Metrics (reference for §16)

| Metric | Baseline |
|---|---|
| Build | **PASS** (`tsc -b && vite build`, executed 2026-11-09; Vite 8.3.5, 2,674 modules) |
| JS gzip (all chunks) | 336.4 KB |
| Entry chunk | 501.78 KB raw / 157.0 KB gzip |
| Chunks | 74 |
| CSS raw / gzip | **235.9 KB / 36.7 KB** |
| Lighthouse Performance | Not measured (Lighthouse not installed) |
| Lighthouse Accessibility | Not measured |
| LCP / CLS | Not measured (no authenticable runtime) |
| Pages rendering without console errors | Only Login rendered; authenticated pages not reachable |
| Visual baseline | Login at 390×844, 768×1024, 1440×900 in `docs/baseline-screenshots/`; 390px capture shows clipped content |

**Measurement limitations:** Playwright, Vitest, Lighthouse and axe are not installed; no backend or test credentials were available for authenticated pages. Authenticated screenshots and Lighthouse/LCP/CLS must be captured in a follow-up environment. Do not treat the missing metrics as passing.

---

## Proposed File Changes

**Created**
- Scoped design-token stylesheet for the new brand, preserving legacy variables until later phases.
- `src/theme/ThemeProvider.tsx` and `useTheme.ts` for a persistent light/dark selection.
- Vitest and Playwright configuration plus focused theme/login tests, all development-only tooling.
- Four committed Playwright login screenshot baselines under `tests/screenshots/` after the new Phase 1 UI is implemented.

**Modified**
- `src/design-system/tokens.css` — scoped theme tokens, theme palette, font faces, and reduced-motion token values; legacy global theme variables remain untouched.
- `src/theme/ThemeProvider.tsx`, `src/theme/ThemeContext.ts`, and `src/theme/useTheme.ts` — isolated theme persistence and context.
- `src/main.tsx` and `index.html` — mount the provider and apply the saved theme before first paint.
- `src/components/auth/Login.tsx` + `Login.css` — sole page restyled in Phase 1, responsive from 320px, both themes.
- `src/components/auth/ThemeToggle.tsx` — accessible theme switch used by Login.
- `src/main.tsx` mounts the theme provider; `App.tsx` and all route/auth logic remain untouched.
- `ClientApp/AGENTS.md` and `ClientApp/README.md` — correct token-storage documentation only.
- Root and `ClientApp/.gitignore` — exclude historical baseline screenshots and generated Playwright reports/results.
- `package.json` and lock file — approved font and dev-only test packages; no new chart package.
- `vitest.config.ts`, `playwright.config.ts`, and the new unit/E2E tests — local, dev-only verification; no CI or authenticated-backend setup.

**Deleted / consolidated**
- No global CSS consolidation or page-wide selector deletion in Phase 1.
- No chart tokens or chart library; the dashboard currently has no charts and the user confirmed none are needed.

**Explicitly untouched**
- API services/contracts, authentication and authorization behavior, page permissions, protected pages other than their inherited existing styling, business rules, and backend/WPF projects.
- `components.json` is intentionally retained: it is harmless IDE/tooling metadata, the project does not use the shadcn CLI, and the user explicitly directed that it remain.

---

## Proposed Implementation Plan

| Phase | Work | Estimate |
|---|---|---|
| 0 | Audit (this report) | 1 day |
| 1 | Scoped tokens, both themes, typography, Login redesign, documentation correction, test-tool setup, and four login visual baselines | 4 days |
| 2 | UI primitives + Vitest component tests | 3 days |
| 3 | Composites (DataTable, Modal, PageHeader, StatCard, states) | 3–4 days |
| 4 | Shell (Sidebar collapse/mobile, Topbar theme, breadcrumbs) | 2 days |
| 5 | Dashboard layout (stat cards, recent activity, quick actions; no charts) | 2 days |
| 6 | Remaining Tier-1 protected pages (15; Login delivered in Phase 1) | 7–9 days |
| 7 | Tier-2 pages (12) | 4–5 days |
| 8 | Responsive hardening at 10 viewports | 2–3 days |
| 9 | Accessibility hardening + axe + reduced-motion and RTL-readiness checks | 2 days |
| 10 | Performance measurement/optimization under stricter budgets | 1–2 days |
| 11 | Playwright workflows, build, lint, and regression | 2 days |
| 12 | Final report | 1 day |

**Updated estimate:** 34–42 engineer-days for one senior frontend engineer. This includes one additional day for local test-tool setup. Authenticated-page visual verification remains stakeholder-owned under Option C; an isolated API account was not provided.

### Acknowledged decision packet (2026-11-09)

- Audit accepted; Phase 0 complete. Phase 1 is authorized by the user's explicit conditional authorization after the confirmations in the decision packet.
- Testing: approved dev-only Vitest, Testing Library, jsdom, Playwright, and `@axe-core/playwright`; optional `@vitest/ui` is omitted to limit install footprint. No CI changes.
- Authenticated access: Option C selected because no isolated backend/test credentials were supplied. Provide a manual QA checklist per page; do not claim implementer visual verification for authenticated pages.
- Fonts: approved variable Fraunces, Inter, and JetBrains Mono, Latin subset only, self-hosted, `font-display: swap`, combined gzip budget ≤150 KB. No Arabic font.
- Localization: English-only; no UI Arabic strings were found in `src/`. Arabic matches are CSS comments only. Write new CSS with logical properties and perform a one-off RTL direction check.
- Historical screenshots stay local and uncommitted; add `docs/baseline-screenshots/` to root `.gitignore`. New Playwright snapshots after Phase 1 live under `ClientApp/tests/screenshots/` and are committed.
- Login is the only page visually redesigned in Phase 1; check at 320, 390, 768, and 1440 CSS pixels. Keep legacy global variables/styles intact so other pages do not change appearance.
- Re-evaluate the four unused Radix dependencies in Phase 3; do not remove them before then. Retain `components.json` as harmless tooling metadata.
- Do not add charts or chart tokens. The dashboard scope is statistics cards, recent activity, and quick actions.
- Phase 9 adds reduced-motion checks. Phase 10 targets: largest/entry chunk ≤140 KB gzip, total JS gzip ≤336 KB, CSS gzip ≤55 KB, and fewer than 100 chunks. If the entry chunk target cannot be met, stop at that gate and justify measured composition.
- The decision packet lists 16 Tier-1 and 11 Tier-2 items but enumerates 12 Tier-2 pages. Since the audited 27-page inventory excludes Login, the consistent total after adding Login is 28 screens: 16 Tier-1 (Login plus 15 protected pages) and 12 Tier-2. The user's high-frequency Tier-1 corrections are accepted; this arithmetic correction is recorded before Phase 6.

---

## Regression Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Auth/session flow accidentally changed while restyling | Users logged out / broken login | Do not touch `AuthContext`, `apiClient`, `permissions`; manual + scripted login/logout checks |
| Route names or permission map changed | Pages inaccessible | Freeze `routeConfig`/`permissions`; diff review |
| API payload/URL drift | Data pages fail | No service edits; review every service diff |
| CSS consolidation removes a still-used selector | Visual breakage on a page | Migrate page-by-page; screenshot compare; grep before delete |
| Light/dark FOUC | Flash, CLS | Inline pre-paint script; verify on reload |
| Theme colors reduce contrast | A11y regression | Use `docs/contrast-matrix.md`; axe scan |
| Bundle grows from new fonts/tokens | Perf regression | Budget ≤10% JS; self-host only approved fonts |
| No automated tests | Regressions undetected | Add tests only with explicit approval (none installed) |

---

## Phase 1 initial execution (2026-11-09; visual direction rejected)

The first implementation pass completed its functional checks, but the stakeholder rejected its visual direction after reviewing the exact viewport captures. The metrics and test counts in this subsection are historical; the post-rejection iteration and current evidence follow below.

- Login is the only page restyled. Visual snapshots exist for light and dark themes at 320, 390, 768, and 1440 CSS pixels (8 PNGs under `ClientApp/tests/screenshots/chromium/`). The snapshots were inspected at 320px, 390px, 768px, and 1440px. No page-level horizontal overflow was observed; the 320px rendering keeps the full form and footer within the scrollable page. Long input placeholders naturally truncate inside the input viewport at narrow widths.
- Playwright update run: **10 passed, 0 failed**; created the 8 initial snapshots. Follow-up non-update run: **10 passed, 0 failed**, including no horizontal overflow, minimum visible text size ≥14px, password visibility, reset guidance, and serious/critical axe violations in both themes.
- Vitest: **1 file passed; 3 tests passed** (theme persistence/toggle, actual theme-toggle control, and scoped token/legacy-variable checks).
- ESLint: `npm.cmd --prefix ClientApp run lint` exited 0.
- Production build: `npm.cmd --prefix ClientApp run build` exited 0 (2,678 modules transformed). It retains Vite's existing large-chunk warning.
- Font payload measured from emitted WOFF2 assets: 125,280 raw bytes; 125,344 bytes when gzip sizes are summed per file (125,305 bytes when the font files are concatenated then gzipped), within the 150 KB budget.
- Build output has 74 JavaScript assets (1,145,758 raw bytes; 344,228 gzip bytes summed per emitted asset = 336.16 KiB) and 5 CSS assets (243,989 raw bytes; 37,943 gzip bytes summed per asset = 37.05 KiB). The JavaScript total is marginally below the Phase 0 recorded 336.4 KiB baseline; it is 0.16 KiB above a literal 336 KiB ceiling if that ceiling uses binary KiB, so reconcile the exact unit/aggregation method at the Phase 10 budget gate. CSS remains below 55 KB.
- Vite reports the largest entry asset at 501.52 KB raw / 156.92 KB gzip. This does **not** meet the stricter ≤140 KB Phase 10 entry target; Phase 1 did not change the existing page chunking strategy. Preserve this as a Phase 10 optimization/gate item, rather than claiming that target is met.
- The new token variables use the isolated `--dvld-design-*` namespace; pre-existing `--dvld-radius-*` and `--dvld-border-strong` variables are not overridden. Native `color-scheme` is scoped to the Login subtree. An E2E check confirms the legacy document root remains in its original dark color scheme while Login is in light mode.
- The Playwright checks stub `/Auth/refresh` with 401. No authenticated account, API service, SQL Server, or database was used. Authenticated pages remain stakeholder-verified under Option C.

### Phase 1 deliverables and follow-up

- Corrected the stale documentation: access token is held in memory and refresh uses an HttpOnly cookie; `localStorage` stores the remembered username and selected theme.
- Updated the historical Phase 0 limitation statements below: they remain an accurate record of the audit-time baseline, not current Phase 1 test coverage or Login screenshot status.
- `components.json` remains intentionally retained as harmless tooling metadata per the user's decision.
- No chart library or chart tokens were introduced.

## Remaining verification limits

- No isolated Admin/Staff credentials or seeded test API were supplied. Authenticated-page visual sign-off is delegated to the stakeholder (Option C); a page-by-page manual checklist is required at the relevant phase gates.
- No authenticated Admin/Staff pages were visually tested by the implementer.
- The Tier-2 count is recorded as 12 because that is the number of pages in the user's explicit list; the 16/11 arithmetic discrepancy should be revisited before Phase 6 if a page was intended to be excluded.

---

## Audit Limitations

The following bullets describe the **Phase 0 audit-time baseline only**; Phase 1 evidence is recorded above.

- Lighthouse, LCP, CLS, and per-page console-error checks were **not executed** (no configured project tooling or authenticated runtime).
- The saved login page captures were visually inspected at 390×844, 768×1024, and 1440×900. Authenticated pages were not rendered or visually tested.
- The original Phase 0 390px capture visibly clipped the login content at the right edge. The Phase 1 Playwright checks and snapshots above document the redesigned Login page.
- No automated suite existed at the time of the Phase 0 audit; Phase 1 introduced and ran the approved local unit and browser suites.
- Current-palette contrast across every component surface was not exhaustively measured; the target palette matrix is complete.

---

## Next Gate

Phase 0 is complete. Phase 1 remains pending visual re-approval after the rejected first design pass. Do not begin Phase 2 until the stakeholder accepts the four revised screenshots. Authenticated-page visual acceptance remains stakeholder-owned. The entry-chunk ≤140 KiB target remains open for Phase 10.

---

## Phase 1 visual iteration after rejection (2026-11-09)

**Status: the requested redesign iteration is implemented and verified; visual sign-off is still pending. Phase 2 has not started.**

### Design corrections

- Light theme now uses Warm Cream `#F3F0E7` as the page and editorial field, Pure White `#FFFFFF` for the form surface, and Deep Charcoal `#202321` for text. The mobile viewport shows the light palette across the brand and form sections.
- Dark theme uses Dark Surface `#171A18`, Deep Charcoal `#202321` for the form surface, Warm Cream `#F3F0E7` for text, and full-strength Acid Lime `#B7E52B` accents.
- Replaced the three bordered feature cards with a vertical editorial list numbered `01`–`03`, separated by 2px rules.
- Changed the desktop composition to an asymmetric 60/40 split. Fraunces display headings use weight 850; the title and content contrast is intentionally stronger.
- Primary CTA, secondary controls, and input fields use 2px borders; the primary CTA has a 4px offset shadow at rest and a 6px hover shadow. Primary surfaces use square corners. The lime CTA remains saturated while disabled.
- Shortened input placeholders so both are fully readable on mobile. At 390×844 the CTA is fully inside the initial viewport. At 320px the page remains vertically scrollable without horizontal overflow.
- No other application page was changed; no API, backend, SQL, or authentication behavior was modified during this iteration.

### Revised visual evidence

The four images below are actual Chrome captures at the exact viewport dimensions. Login refresh is stubbed; no backend or database is used.

| Theme | 390×844 | 1440×900 |
|---|---|---|
| Light | `ClientApp/tests/screenshots/phase1-signoff/login-light-390x844.png` | `ClientApp/tests/screenshots/phase1-signoff/login-light-1440x900.png` |
| Dark | `ClientApp/tests/screenshots/phase1-signoff/login-dark-390x844.png` | `ClientApp/tests/screenshots/phase1-signoff/login-dark-1440x900.png` |

Playwright assertions verify the computed page/surface colors, exact lime CTA color, 2px input and CTA borders, CTA shadow, heading weight ≥700, asymmetric desktop columns, numbered service list, minimum text size ≥14px, and full mobile CTA visibility at 390×844.

Independent PNG pixel samples from the exact-size mobile captures confirm light page `#F3F0E7`, light form `#FFFFFF`, dark page `#171A18`, dark form `#202321`, and the primary CTA `#B7E52B` in both themes.

### Verification

- Production build: `npm.cmd --prefix ClientApp run build` — exit 0; 2,678 modules transformed. Vite still emits its existing large-chunk warning.
- ESLint: `npm.cmd --prefix ClientApp run lint` — exit 0.
- Vitest: 1 test file, **3 passed**.
- Playwright baseline update: **12 passed**. Follow-up non-update run: **12 passed, 0 failed**, covering four viewports in both themes, interaction behavior, axe checks in both themes, visual-token assertions, and reduced-motion transition behavior.
- `git diff --check` reported no whitespace errors. Git emitted only its existing LF-to-CRLF advisory for tracked files.
- Screenshots and browser checks use local Vite and a stubbed `/Auth/refresh` 401. No API login, SQL Server connection, or database operation occurred.

### Bundle report (all values in KiB; 1 KiB = 1,024 bytes)

| Metric | Phase 0 baseline | Current iteration | Target | Status |
|---|---:|---:|---:|---|
| Total JavaScript gzip, emitted files summed | 336.40 | 335.97 | ≤336.00 | Pass |
| Largest entry JavaScript gzip | 153.32* | 151.65 | ≤140.00 | Open for Phase 10 |
| Total CSS gzip, emitted files summed | 36.70 | 37.28 | ≤55.00 | Pass |
| Combined font payload gzip, font files summed | — | 122.41 | ≤150.00 | Pass |

`*` The Phase 0 entry figure was reported by Vite as 157.0 kB; it is converted here to 153.32 KiB. The current entry figure is measured from the emitted asset's gzip bytes. The measured entry remains 11.65 KiB above the 140 KiB Phase 10 gate. Total JS is 0.43 KiB below the 336 KiB budget and the 336.40 KiB baseline.

The first visual pass was rejected. This iteration has not yet been accepted by the stakeholder. Wait for visual review of the four revised captures before Phase 2.
