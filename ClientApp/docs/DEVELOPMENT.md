# ClientApp Development Guide

This guide covers local development workflows, compiler and linter configurations, and conventions for developing and extending `ClientApp`.

---

## 1. Local Development Workflow

### Step 1: Start `DVLD.Api`
`ClientApp` expects the backend API to be reachable at `http://localhost:5260/api`. From the repository root:

```powershell
dotnet run --project ".\API\DVLD.Api\DVLD.Api.csproj" --launch-profile http
```

### Step 2: Run the Vite Development Server
In a second terminal, from `ClientApp/`:

```powershell
cd ClientApp
npm install
npm run dev
```

Vite serves the application at `http://localhost:5173`, which matches the `"ReactApp"` CORS policy in `API/DVLD.Api/Program.cs`.

---

## 2. Build and Quality Verification

| Command | Underlying Tool | Description |
|---|---|---|
| `npm run dev` | `vite` | Starts local HMR dev server (`http://localhost:5173`). |
| `npm run build` | `tsc -b && vite build` | Runs TypeScript project-reference type checking (`tsc -b`) followed by production bundling into `dist/`. |
| `npm run lint` | `eslint .` | Runs ESLint flat config (`eslint.config.js`) across all `.ts` and `.tsx` source files. |
| `npm run preview` | `vite preview` | Serves the built production bundle locally for verification (`http://localhost:4173`). |

> **PowerShell Execution Note (Windows):** If PowerShell execution policies restrict `npm.ps1`, invoke `npm.cmd` directly (e.g. `npm.cmd run lint` or `npm.cmd run build`).

### Current Verification Status
- `npm run build`: **Passing (Exit code 0)** — Type-check and bundling complete in under 1 second.
- `npm run lint`: **Passing (Exit code 0)** — Zero ESLint errors or warnings across all source files.
- `git diff --check`: **Passing (Exit code 0)** — Zero whitespace errors or conflict markers.

---

## 3. TypeScript & ESLint Configuration

- **TypeScript (`tsconfig.app.json`):**
  - Target: `ES2023`, `DOM`
  - Module resolution: `bundler` with `verbatimModuleSyntax: true` (use `import type` for type-only imports)
  - Strict checks enabled: `noUnusedLocals`, `noUnusedParameters`, `erasableSyntaxOnly`, `noFallthroughCasesInSwitch`
  - Path alias: `@/*` mapped to `./src/*`
- **ESLint (`eslint.config.js`):**
  - Uses `@eslint/js`, `typescript-eslint`, `eslint-plugin-react-hooks` (flat recommended rules), and `eslint-plugin-react-refresh`.

---

## 4. Development Conventions

1. **Centralized HTTP Services (`src/services/`):**
   - Route all backend API requests through `src/services/apiClient.ts` using `apiRequest<T>()`.
   - Never call `fetch()` directly inside React UI components.
   - Attach bearer tokens via `accessToken: string`.
   - Parse RFC 7807 `ProblemDetails` via `ApiError` for user-facing alert messages.
2. **Role-Based Access Control (RBAC):**
   - Check endpoint permissions in backend controller attributes (`StaffOnly`, `AdminOnly`, `StaffOrAdmin`).
   - Guard view rendering via `isPagePermitted()` in `AppRouter.tsx`.
   - Conditionally render navigation items in `Sidebar.tsx`.
3. **State Management & Custom Hooks:**
   - Keep page-level data fetching inside dedicated hooks (`src/hooks/`).
   - Use `useModal()` for controlled dialog open/close lifecycles.
   - Use `usePagination()` and `useTableFilters()` for table search and slicing.
4. **Shared UI Components (`src/components/common/`):**
   - Reuse standardized design system elements: `DataTable`, `TableToolbar`, `StatusBadge`, `StatCard`, `PageHero`, `ErrorAlert`, `EmptyState`.
