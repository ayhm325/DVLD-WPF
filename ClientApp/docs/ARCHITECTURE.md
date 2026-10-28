# ClientApp Architecture Guide

This document describes the internal structure, component hierarchy, state model, and navigation design of the React frontend (`ClientApp/`).

---

## 1. Directory Structure

```text
ClientApp/
├── public/                  # Static public assets
├── src/
│   ├── assets/              # Bundled static assets
│   ├── components/          # UI components and modals
│   │   ├── AddPersonModal.tsx
│   │   ├── Login.tsx
│   │   ├── PeopleTable.tsx
│   │   ├── Sidebar.tsx
│   │   └── Topbar.tsx
│   ├── services/            # HTTP service modules calling DVLD.Api
│   │   ├── authService.ts
│   │   ├── countryService.ts
│   │   ├── driverService.ts
│   │   ├── peopleService.ts
│   │   └── personService.ts
│   ├── types/               # Shared TypeScript domain models
│   │   └── Person.ts
│   ├── App.css              # Application layout, dashboard, table, and modal styles
│   ├── App.tsx              # Root state coordinator and view switcher
│   ├── index.css            # Base reset and typography styles
│   └── main.tsx             # React DOM entry point (StrictMode + createRoot)
├── docs/                    # React-specific documentation
├── eslint.config.js         # Flat ESLint configuration
├── index.html               # HTML shell
├── package.json             # Dependencies and npm scripts
├── tsconfig.json            # Solution-style TypeScript config references
├── tsconfig.app.json        # Application TypeScript compiler options
├── tsconfig.node.json       # Vite config TypeScript compiler options
└── vite.config.ts           # Vite configuration (@vitejs/plugin-react)
```

---

## 2. Component Hierarchy

```text
main.tsx
  └── App.tsx
        ├── Login.tsx (rendered when accessToken === null)
        └──Authenticated Shell (rendered when accessToken !== null)
              ├── Sidebar.tsx
              └── div.main-area
                    ├── Topbar.tsx
                    ├── main.content
                    │     ├── Dashboard view (inline in App.tsx)
                    │     ├── PeopleTable.tsx (when activePage === 'People')
                    │     ├── Drivers summary panel (inline in App.tsx)
                    │     └── Settings placeholder panels (inline in App.tsx)
                    ├── footer.app-footer
                    └── AddPersonModal.tsx (rendered when isAddPersonOpen === true)
```

---

## 3. Component Responsibilities

| Component | File | Responsibility |
|---|---|---|
| `App` | `src/App.tsx` | Holds top-level authentication token (`accessToken`), active navigation state (`activePage`), loaded `people` and `drivers` arrays, loading/error states, client-side search string (`searchName`), and modal visibility (`isAddPersonOpen`). |
| `Login` | `src/components/Login.tsx` | Controlled login form (`userName`, `password`). Calls `login()` from `authService.ts` and passes `result.accessToken` to `onLoginSuccess`. Displays an Arabic error message (`اسم المستخدم أو كلمة المرور غير صحيحة.`) on failure. |
| `Sidebar` | `src/components/Sidebar.tsx` | Collapsible navigation rail (`collapsed` state) with primary items (`Dashboard`, `People`, `Drivers`, `Licenses`, `Applications`) and collapsible `Settings` sub-items (`Application Types`, `License Classes`, `Test Types`, `Countries`). |
| `Topbar` | `src/components/Topbar.tsx` | Header bar displaying breadcrumb (`DVLD / Management`), the current `activePage` title, an online status badge, and a static avatar badge (`A`). |
| `PeopleTable` | `src/components/PeopleTable.tsx` | Renders the people count badge, `+ Add Person` trigger button, search input (`searchName`), and a 5-column table (`ID`, `Full Name`, `National No.`, `DATE OF BIRTH`, `Gender`). |
| `AddPersonModal` | `src/components/AddPersonModal.tsx` | Modal dialog (`role="dialog"`, `aria-modal="true"`) that fetches countries via `getCountries(accessToken)`, validates form inputs (age >= 18, 10-digit national number, Jordanian mobile format), previews an optional photo, and submits `createPerson(accessToken, ...)`. |

---

## 4. State Management & Data Flow

`ClientApp` uses local React hooks (`useState`, `useEffect`, `useCallback`) without an external state library or client-side router:

1. **Authentication State (`App.tsx:19`):**
   - `accessToken` (`string | null`) determines whether `<Login />` or the authenticated application shell is rendered.
   - Because `accessToken` is stored in React memory only (not `localStorage` or `sessionStorage`), refreshing the browser resets the session to the login screen.
2. **Data Fetching Lifecycle (`App.tsx:29-98`):**
   - `loadPeople` and `loadDrivers` are memoized with `useCallback` and depend on `accessToken`.
   - When `accessToken` is set, an effect (`App.tsx:67-98`) triggers `loadDashboardData`, which invokes `loadPeople()` and `loadDrivers()` sequentially.
   - Navigating manually to `'Dashboard'` or `'People'` calls `void loadPeople()`, and navigating to `'Drivers'` calls `void loadDrivers()` (`App.tsx:116-127`).
3. **Client-Side Navigation (`App.tsx:18, 116-127`):**
   - Navigation is state-driven via `activePage: string` rather than URL routes (`react-router` is not installed).
   - See [`FEATURE-STATUS.md`](FEATURE-STATUS.md) for current navigation rendering behavior on unimplemented views.
