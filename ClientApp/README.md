# DVLD — React Frontend (`ClientApp`)

`ClientApp` is the browser-based React + TypeScript + Vite single-page application for the Driving & Vehicle License Department (DVLD) system. It communicates with `DVLD.Api` over HTTP + JSON + JWT (`http://localhost:5260/api`).

> **Development Status:** This frontend was recently introduced (commit `4f75658`) and is under active, iterative development. Core authentication and people directory workflows are implemented, while other views are partially implemented or currently placeholders. See [`docs/FEATURE-STATUS.md`](docs/FEATURE-STATUS.md) for the complete evidence-based feature matrix.

---

## 1. Technology Stack

| Layer | Technology / Version (`package.json`) |
|---|---|
| UI Library | React `^19.3.1`, React DOM `^19.3.1` |
| Language | TypeScript `~6.0.3` |
| Bundler & Dev Server | Vite `^8.3.4` (`@vitejs/plugin-react` `^6.0.1`) |
| Linter | ESLint `^9.39.4`, `typescript-eslint` `^8.58.0`, `eslint-plugin-react-hooks` `^7.0.1` |
| HTTP Client | Browser `fetch` API |
| Styling | Custom CSS (`src/App.css`, `src/index.css`) |

---

## 2. Prerequisites

1. **Node.js** (compatible with Vite 8 / TypeScript 6).
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

# Start the Vite development server (defaults to http://localhost:5173)
npm run dev

# Type-check (tsc -b) and build production assets into dist/
npm run build

# Run ESLint across TypeScript/React source files
npm run lint

# Preview the production build locally
npm run preview
```

---

## 4. High-Level Feature Status Summary

| Feature / View | Status | Summary |
|---|---|---|
| **Login (`POST /api/Auth/login`)** | `Implemented` | Authenticates via `authService.ts` and stores `accessToken` in React memory state (`App.tsx`). |
| **People Directory & Search** | `Implemented` | Loads `GET /api/People` (`peopleService.ts`), displays table (`PeopleTable.tsx`), and filters client-side by name or national number. |
| **Add Person Modal** | `Partially Implemented` | Loads `GET /api/Countries` (`countryService.ts`) and submits `POST /api/People` (`personService.ts`). Photo selection is client-side preview only (`imagePath: null` is sent). |
| **Dashboard** | `Partially Implemented` | Displays live counts for `Total People` and `Total Drivers`, plus a 5-row recent people preview. `Licenses` and `Applications` cards are hardcoded to `0` (`"Connect API to load data"`). |
| **Drivers View** | `Partially Implemented` | Fetches `GET /api/Drivers` (`driverService.ts`), but renders a `.placeholder-panel` displaying only `"Total drivers: {drivers.length}"`. |
| **Settings Pages (`Application Types`, `License Classes`, `Test Types`, `Countries`)** | `Not Implemented` (`Placeholder`) | Renders static description cards with a `"Back to Dashboard"` button. |
| **`Licenses` & `Applications` Pages** | `Not Implemented` (`Placeholder`) | Listed in `Sidebar.tsx`, but excluded from the fallback condition in `App.tsx` without a corresponding render block, resulting in a blank content area when clicked. |

---

## 5. Detailed React Documentation (`ClientApp/docs/`)

Detailed frontend documentation is maintained inside `ClientApp/docs/` so it can evolve independently with each frontend iteration:

- [**Architecture Guide** (`docs/ARCHITECTURE.md`)](docs/ARCHITECTURE.md) — Project layout, component hierarchy, state management, and navigation flow.
- [**API Integration & Authentication** (`docs/API-INTEGRATION.md`)](docs/API-INTEGRATION.md) — Service modules, endpoints consumed, JWT handling, CORS, and backend authorization policy interactions (`StaffOrAdmin` vs `StaffOnly`).
- [**Feature Status Matrix** (`docs/FEATURE-STATUS.md`)](docs/FEATURE-STATUS.md) — Detailed classification of implemented, partially implemented, and unimplemented features, plus observed limitations.
- [**Development Guide** (`docs/DEVELOPMENT.md`)](docs/DEVELOPMENT.md) — Local workflow, TypeScript/ESLint configuration, current lint observations, and guidance for extending the frontend.

For backend, database, security, and WPF desktop documentation, see the root [`README.md`](../README.md) and [`docs/`](../docs/SYSTEM-DOCUMENTATION.md).
