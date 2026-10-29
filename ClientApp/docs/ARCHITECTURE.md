# ClientApp Architecture Guide

This document describes the internal structure, component hierarchy, state model, navigation design, and authentication lifecycle of the React frontend (`ClientApp/`).

---

## 1. Directory Structure

```text
ClientApp/
├── public/                  # Static public assets (favicon.svg, icons.svg)
├── src/
│   ├── assets/              # Bundled static assets
│   ├── context/             # Authentication & session state management
│   │   ├── authContextInstance.ts # React AuthContext definition
│   │   ├── AuthContext.tsx        # AuthProvider implementation
│   │   └── useAuth.ts            # Typed useAuth consumer hook
│   ├── hooks/               # Shared React hooks (useAuth)
│   ├── components/          # UI components, pages, and modals
│   │   ├── common/          # Shared enterprise UI components
│   │   │   ├── ConfirmModal.tsx   # Accessible confirmation dialog
│   │   │   ├── LoadingSpinner.tsx # Animated status spinner
│   │   │   └── StatusBadge.tsx    # Semantic status badge pill
│   │   ├── international license/ # International license management
│   │   │   ├── InternationalLicensesPage.tsx
│   │   │   ├── InternationalLicenseTable.tsx
│   │   │   ├── IssueInternationalLicensePage.tsx
│   │   │   └── international-licenses.css
│   │   ├── local applications/    # Local driving license applications
│   │   │   ├── LocalApplicationsPage.tsx
│   │   │   ├── LocalApplicationsList.tsx
│   │   │   ├── LocalApplicationForm.tsx
│   │   │   └── local-applications.css
│   │   ├── AddPersonModal.tsx
│   │   ├── ApplicationTypesPage.tsx
│   │   ├── CountriesPage.tsx
│   │   ├── DriversPage.tsx
│   │   ├── LicenseClassesPage.tsx
│   │   ├── LicensesPage.tsx
│   │   ├── Login.css
│   │   ├── Login.tsx
│   │   ├── PeopleTable.tsx
│   │   ├── Sidebar.tsx
│   │   ├── TestTypesPage.tsx
│   │   └── Topbar.tsx
│   ├── services/            # HTTP service modules calling DVLD.Api
│   │   ├── apiClient.ts     # Unified HTTP client with 401 interceptor
│   │   ├── authService.ts   # Authentication & profile endpoints
│   │   ├── countryService.ts
│   │   ├── dashboardService.ts
│   │   ├── driverService.ts
│   │   ├── internationalLicenseService.ts
│   │   ├── licenseClassService.ts
│   │   ├── licenseService.ts
│   │   ├── localDrivingLicenseApplicationService.ts
│   │   ├── peopleService.ts
│   │   └── personService.ts
│   ├── types/               # TypeScript domain models and DTO contracts
│   │   ├── Auth.ts
│   │   ├── InternationalLicense.ts
│   │   ├── LocalDrivingLicenseApplication.ts
│   │   └── Person.ts
│   ├── App.css              # Design tokens, layout, typography, and controls
│   ├── App.tsx              # Root view coordinator and page router
│   ├── index.css            # Base reset and typography styles
│   └── main.tsx             # Entry point (StrictMode + AuthProvider + createRoot)
├── docs/                    # React frontend architecture & feature documentation
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
  └── AuthProvider (context/AuthContext.tsx)
        └── App.tsx
              ├── Login.tsx (rendered when !isAuthenticated)
              └── Authenticated Shell (rendered when isAuthenticated)
                    ├── Sidebar.tsx
                    │     ├── Nav items (Dashboard, People, Drivers, Licenses, International, Local Applications)
                    │     ├── Settings submenu (Application Types, License Classes, Test Types, Countries)
                    │     └── User Profile card + Quick Sign Out button
                    └── main.main-content
                          ├── Topbar.tsx
                          │     ├── Breadcrumb & Page title
                          │     ├── System status badge (Online)
                          │     └── User menu dropdown with user details & Sign Out action
                          ├── main.page-content
                          │     ├── Dashboard view (welcome banner, stat cards from /Dashboard/statistics, PeopleTable)
                          │     ├── PeopleTable.tsx (when activePage === 'People')
                          │     ├── DriversPage.tsx (when activePage === 'Drivers')
                          │     ├── LicensesPage.tsx (when activePage === 'Licenses')
                          │     ├── InternationalLicensesPage.tsx (when activePage === 'International Licenses')
                          │     ├── IssueInternationalLicensePage.tsx (when activePage === 'International Licenses' & issuing)
                          │     ├── LocalApplicationsPage.tsx (when activePage === 'Local Applications')
                          │     ├── CountriesPage.tsx (when activePage === 'Countries')
                          │     ├── ApplicationTypesPage.tsx (when activePage === 'Application Types')
                          │     ├── LicenseClassesPage.tsx (when activePage === 'License Classes')
                          │     └── TestTypesPage.tsx (when activePage === 'Test Types')
                          ├── AddPersonModal.tsx (rendered when isAddPersonOpen === true)
                          ├── ConfirmModal.tsx (rendered when isLogoutModalOpen === true)
                          └── footer.app-footer
```

---

## 3. Authentication & Logout Architecture

1. **Authentication State & Session Storage (`src/context/`)**:
   - `AuthProvider` maintains `accessToken`, `user` (`userId`, `userName`, `personId`, `fullName`, `role`), and session expiration (`expiresAtUtc`).
   - Session data is persisted in `sessionStorage` under `'dvld_auth_session'`.
   - On page load, `loadStoredSession()` restores the authenticated session if the token has not expired (`new Date(expiresAtUtc) > Date.now()`).
   - If the token is expired or corrupt, the storage is cleaned automatically.

2. **Logout Workflow**:
   - Logout is triggered from:
     - The **Topbar user profile dropdown menu** ("Sign Out" button).
     - The **Sidebar footer** (quick sign-out icon).
     - The **Logout Confirmation Dialog** (`ConfirmModal.tsx`).
     - Automatic 401 Unauthorized interceptor in `apiClient.ts`.
   - When logout executes:
     1. `sessionStorage.removeItem('dvld_auth_session')` is invoked.
     2. In-memory `session` state is cleared (`null`).
     3. User state is reset to `null`.
     4. Navigation returns to `'Dashboard'`, and view immediately shifts to `<Login />`.
     5. A friendly message (`authMessage`) is presented to the user on the login screen.
     6. Subsequent requests contain no Authorization header.
     7. Browser back navigation cannot restore authenticated views because `isAuthenticated` is `false`.

3. **401 Unauthorized Interception**:
   - `apiClient.ts` provides `setUnauthorizedHandler()`.
   - When any API request receives an HTTP `401 Unauthorized`, it automatically invokes `logout('Session expired. Please log in again.')`, preventing stale-token loops.

---

## 4. State Management & Navigation

- Client-side navigation is state-driven via `activePage: string` in `App.tsx`.
- Modals (`AddPersonModal`, `ConfirmModal`) are conditionally mounted with focus management and Esc-key support.
- API service calls are centralized in `src/services/` using the unified `apiRequest<T>()` transport client.
