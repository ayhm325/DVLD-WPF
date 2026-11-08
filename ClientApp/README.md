# DVLD — React Frontend (`ClientApp`)

`ClientApp` is the browser-based React + TypeScript + Vite single-page application for the Driving & Vehicle Licensing Department (DVLD) system. It communicates with `DVLD.Api` over HTTP + JSON + JWT (`http://localhost:5260/api`).

> **Operational Status:** `ClientApp` is an active administrative web portal. All core departmental views, authentication flows, management tables, settings modals, and navigation workflows are implemented as baseline foundations. Governance rules and development standards are maintained in [`AGENTS.md`](./AGENTS.md) and [`docs/domain/LICENSING-DOMAIN-SPECIFICATION.md`](../docs/domain/LICENSING-DOMAIN-SPECIFICATION.md).

---

## 1. Technology Stack

| Layer | Technology / Version (`package.json`) | Purpose |
|---|---|---|
| **UI Library** | React `^19.3.1`, React DOM `^19.3.1` | Core declarative component architecture |
| **Language** | TypeScript `~6.0.3` | Strict type safety and compilation |
| **Styling** | Tailwind CSS `^4.3.3` (`@tailwindcss/vite`), Custom Design System (`App.css`, `index.css`) | Institutional theme, design tokens, utility classes |
| **Icons & Primitives** | `lucide-react` `^1.65.0`, `@radix-ui/react-*` | Accessible UI primitives and iconography |
| **Bundler & Dev Server** | Vite `^8.3.4` (`@vitejs/plugin-react` `^6.1.2`) | Fast HMR dev server and production asset bundling |
| **Linter** | ESLint `^10.13.0`, `typescript-eslint` `^8.71.2`, `eslint-plugin-react-hooks` `^7.1.1` | Code quality and React 19 rules enforcement |
| **HTTP Transport** | Unified API Client (`src/services/apiClient.ts`) on browser `fetch` | Centralized base URL, JWT bearer headers, error normalization, and 401 interception |

---

## 2. Prerequisites

1. **Node.js** (v20+ recommended, compatible with Vite 8 / TypeScript 6).
2. **Running `DVLD.Api` Backend**:
   - The React app sends requests to `http://localhost:5260/api`.
   - `DVLD.Api` configures the `"ReactApp"` CORS policy (`API/DVLD.Api/Program.cs`) allowing cross-origin requests from `http://localhost:5173` (Vite's default dev server origin).
   - Start the backend API from the repository root before using `ClientApp`:
     ```powershell
     dotnet run --project ".\API\DVLD.Api\DVLD.Api.csproj" --launch-profile http
     ```

---

## 3. Setup and Available Scripts

Run all frontend commands from the `ClientApp` directory:

```powershell
cd ClientApp

# Install dependencies
npm install

# Start the Vite development server (http://localhost:5173)
npm run dev

# Type-check (tsc -b) and build production assets into dist/
npm run build

# Run ESLint across TypeScript and React source files
npm run lint

# Preview the production build locally (http://localhost:4173)
npm run preview
```

> **Windows PowerShell Execution Policy Note:** If Windows PowerShell execution policy restricts running `npm.ps1`, invoke the command line wrapper directly (e.g. `npm.cmd run dev`, `npm.cmd run build`, `npm.cmd run lint`).

---

## 4. Feature Implementation Summary

> [!NOTE]
> The feature matrix below reflects verified baseline frontend foundations established at commit `29e43d6` (historical 2026-11-01). Ongoing automated end-to-end and integration verification across all views is tracked under Phase 12 in [`ENGINEERING-ROADMAP.md`](../ENGINEERING-ROADMAP.md).

| Feature / View | Status | Implementation Details |
|---|---|---|
| **Authentication & Login** | `Implemented` | Clean, high-contrast light-theme portal (`Login.tsx`, `Login.css`), JWT storage in `sessionStorage`, Remember Me in `localStorage`, error alerts, and password visibility toggle. |
| **Session Lifecycle & 401 Auto-Logout** | `Implemented` | `AuthContext.tsx` validates token expiration on refresh; `apiClient.ts` intercepts HTTP 401 and triggers automated session termination. |
| **Dashboard** | `Implemented` | Renders unified statistics cards (`GET /api/Dashboard/statistics`), recent people overview, and quick-action shortcuts. |
| **People Management** | `Implemented` | Comprehensive 5-column paginated table (`PeopleTable.tsx`), search filtering by name and national number, and details modal. |
| **Add Person Workflow** | `Implemented` | Full modal form (`AddPersonModal.tsx`) with field validation, country selection, photo preview, and `POST /api/People` integration. |
| **Drivers Directory** | `Implemented` | Drivers list (`DriversPage.tsx`) displaying national numbers, full names, active license counts, and registration dates. |
| **Local Licenses** | `Implemented` | Full licenses register (`LicensesPage.tsx`) with class badges, issue/expiration dates, and active status indicators. |
| **International Licenses** | `Implemented` | Directory (`InternationalLicensesPage.tsx`) and issuance form (`IssueInternationalLicensePage.tsx`) with eligibility verification. |
| **Local License Applications** | `Implemented` | Paginated applications table (`LocalApplicationsPage.tsx`) with status filters, search, and new application stepper. |
| **Settings — Countries** | `Implemented` | Complete country directory (`CountriesPage.tsx`) with instant search. |
| **Settings — Application Types** | `Implemented` | List of application types (`ApplicationTypesPage.tsx`) with modal to edit titles and fees via `PUT /api/ApplicationTypes/{id}`. |
| **Settings — License Classes** | `Implemented` | Complete license classes directory (`LicenseClassesPage.tsx`) showing minimum ages, validity periods, and fees. |
| **Settings — Test Types** | `Implemented` | Test types management (`TestTypesPage.tsx`) with modal to edit test fees and descriptions via `PUT /api/TestTypes/{id}`. |

---

## 5. Frontend Governance & Target Architecture References

Authoritative documentation for `ClientApp` is organized under the repository governance suite:

- [**Frontend Agent Governance** (`ClientApp/AGENTS.md`)](./AGENTS.md) — Operational instructions, TypeScript standards, and component isolation rules.
- [**Engineering Standards** (`ENGINEERING-STANDARDS.md`)](../ENGINEERING-STANDARDS.md) — System architecture, Clean Architecture layers, API contracts, persistence, and concurrency rules.
- [**Licensing Domain Specification** (`docs/domain/LICENSING-DOMAIN-SPECIFICATION.md`)](../docs/domain/LICENSING-DOMAIN-SPECIFICATION.md) — Authoritative domain business invariants and licensing workflow constraints.
- [**Security Standards** (`SECURITY-STANDARDS.md`)](../SECURITY-STANDARDS.md) — Session storage threat model, per-tab isolation, XSS prevention, and RBAC policies.
- [**Preserved Target Architecture Archive** (`docs/archive/historical-planning/TARGET-ARCHITECTURE.md`)](../docs/archive/historical-planning/TARGET-ARCHITECTURE.md) — Preserved historical target architecture draft and media pipeline flowchart.
