# DVLD — React Frontend (`ClientApp`)

`ClientApp` is the browser-based React + TypeScript + Vite single-page application for the Driving & Vehicle Licensing Department (DVLD) system. It communicates with `DVLD.Api` over HTTP + JSON + JWT (`http://localhost:5260/api`).

> **Operational Status:** `ClientApp` is an active, fully functional administrative web portal. All core departmental views, authentication flows, management tables, settings modals, and navigation workflows are implemented and verified against the backend API. See [`docs/FEATURE-STATUS.md`](docs/FEATURE-STATUS.md) for the complete feature matrix.

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

---

## 4. Feature Implementation Summary

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

## 5. Documentation Directory (`ClientApp/docs/`)

Authoritative documentation for `ClientApp` is organized under [`ClientApp/docs/`](docs/):

- [**Architecture Guide** (`docs/ARCHITECTURE.md`)](docs/ARCHITECTURE.md) — Directory layout, component hierarchy, state management, and navigation routing.
- [**API Integration & Security** (`docs/API-INTEGRATION.md`)](docs/API-INTEGRATION.md) — Service modules, endpoint mapping, JWT handling, error normalization, and RBAC policy interactions.
- [**Feature Status Matrix** (`docs/FEATURE-STATUS.md`)](docs/FEATURE-STATUS.md) — Canonical status matrix for all frontend features, endpoints, and components.
- [**Development Guide** (`docs/DEVELOPMENT.md`)](docs/DEVELOPMENT.md) — Local workflows, TypeScript strict conventions, ESLint rules, and extension guidelines.
- [**Security Policy** (`SECURITY.md`)](SECURITY.md) — Session storage threat model, XSS prevention audit, CSP headers, and RBAC role mapping.
