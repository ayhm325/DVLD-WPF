# ClientApp Development Guide

This guide covers local development workflows, compiler and linter configurations, and conventions for extending the React frontend (`ClientApp/`).

---

## 1. Local Development Workflow

### Step 1: Start `DVLD.Api`
`ClientApp` expects the backend API to be reachable at `http://localhost:5260/api`. From the repository root:

```powershell
dotnet run --project ".\API\DVLD.Api\DVLD.Api.csproj" --launch-profile http
```

### Step 2: Run the Vite Dev Server
In a second terminal, from `ClientApp/`:

```powershell
cd ClientApp
npm install
npm run dev
```

Vite serves the application at `http://localhost:5173`, which matches the `"ReactApp"` CORS policy in `API/DVLD.Api/Program.cs`.

---

## 2. Build and Quality Checks

| Command | Underlying Tool | Behavior |
|---|---|---|
| `npm run dev` | `vite` | Starts local HMR dev server (`http://localhost:5173`). |
| `npm run build` | `tsc -b && vite build` | Runs TypeScript project-reference type checking followed by production bundling into `ClientApp/dist/`. |
| `npm run lint` | `eslint .` | Runs ESLint flat config (`eslint.config.js`) across all `.ts` and `.tsx` files. |
| `npm run preview` | `vite preview` | Serves the built `dist/` bundle locally. |

> **PowerShell Execution Policy Note (Windows):** If PowerShell blocks `npm.ps1` due to script execution policy, invoke `npm.cmd` directly (for example, `npm.cmd run lint` or `npm.cmd run dev`).

### Current Verification Status (Commit `4f75658`)
- `npm.cmd --prefix ClientApp run lint` currently exits with **1 error, 0 warnings** at `src/components/AddPersonModal.tsx:67:7` (`react-hooks/set-state-in-effect` due to calling `setImagePreview('')` synchronously inside `useEffect` when `!imageFile`).
- `azure-pipelines.yml` at the repository root currently builds and tests the `.NET 8` solution (`Presentation (WPF)/DVLD_WPF.sln`) and does not yet execute `npm ci`, `npm run lint`, or `npm run build` for `ClientApp`.

---

## 3. TypeScript & ESLint Configuration

- **TypeScript (`tsconfig.app.json`):**
  - Target: `ES2023`, `DOM`
  - Module resolution: `bundler` with `verbatimModuleSyntax: true` (always use `import type` for type-only imports)
  - Strict checks enabled: `noUnusedLocals`, `noUnusedParameters`, `erasableSyntaxOnly`, `noFallthroughCasesInSwitch`
- **ESLint (`eslint.config.js`):**
  - Uses `@eslint/js`, `typescript-eslint`, `eslint-plugin-react-hooks` (v7 flat recommended rules, including React 19 effect rules), and `eslint-plugin-react-refresh`.

---

## 4. Guidelines for Extending `ClientApp`

1. **Adding or Consolidating API Services (`src/services/`):**
   - Keep HTTP calls inside `src/services/` rather than calling `fetch` directly inside components.
   - Pass `accessToken: string` and include `Authorization: Bearer ${accessToken}`.
   - Parse `application/problem+json` responses (`detail` and `title` fields) for user-facing error messages, following the pattern in `src/services/personService.ts:37-68`.
   - Consider consolidating `peopleService.ts` and `personService.ts` when refactoring people operations.
2. **Respecting Backend Authorization Policies:**
   - Before calling an endpoint on initial load, check its policy in [`../../docs/API.md`](../../docs/API.md). Endpoints marked `StaffOnly` return `403 Forbidden` for `Admin` users, and `AdminOnly` endpoints return `403 Forbidden` for `Staff` users.
3. **Updating Documentation:**
   - Whenever a view transitions from `Not Implemented` (`Placeholder`) or `Partially Implemented` to `Implemented`, update [`FEATURE-STATUS.md`](FEATURE-STATUS.md) and [`API-INTEGRATION.md`](API-INTEGRATION.md) in the same change.
