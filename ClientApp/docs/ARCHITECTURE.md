# ClientApp Architecture Guide

This document describes the internal structure, component hierarchy, state management model, navigation routing, and authentication lifecycle of `ClientApp`.

---

## 1. Directory Structure

```text
ClientApp/
├── public/                  # Static assets served as-is (favicon.svg, icons.svg)
├── src/
│   ├── assets/              # Static media assets (hero.png)
│   ├── context/             # Authentication & session state management
│   │   ├── authContextInstance.ts # React AuthContext definition
│   │   ├── AuthContext.tsx        # AuthProvider implementation & storage sync
│   │   └── useAuth.ts             # Typed useAuth consumer hook
│   ├── hooks/               # Custom reusable React hooks
│   │   ├── index.ts               # Barrel export for hooks
│   │   ├── useAsyncData.ts        # Generic async fetcher with loading/error state
│   │   ├── useDashboardStatistics.ts # Real-time dashboard stats hook
│   │   ├── useDebounce.ts         # Input debounce hook for search queries
│   │   ├── useModal.ts            # Controlled modal open/close hook
│   │   ├── usePagination.ts       # Table pagination state and slicing
│   │   ├── usePeople.ts           # People list lifecycle and filtering
│   │   └── useTableFilters.ts     # Generic table search and column filters
│   ├── components/          # UI components, pages, modals, and layouts
│   │   ├── common/          # Shared enterprise UI design system
│   │   │   ├── AccessDenied.tsx       # RBAC 403 access restriction screen
│   │   │   ├── CommandPalette.tsx     # Global Ctrl+K command search palette
│   │   │   ├── ConfirmModal.tsx       # Accessible action confirmation modal
│   │   │   ├── DataTable.tsx          # Reusable sorting/paginated data table
│   │   │   ├── DataTableContainer.tsx  # Responsive table wrapper shell
│   │   │   ├── DataTableRowContextMenu.tsx # Contextual row action popup
│   │   │   ├── EmptyState.tsx         # Zero-data presentation display
│   │   │   ├── ErrorAlert.tsx         # User-facing error message banner
│   │   │   ├── LoadingSpinner.tsx     # Animated loading indicator
│   │   │   ├── ModalShell.tsx         # Accessible dialog container shell
│   │   │   ├── PageHero.tsx           # Page header banner with breadcrumbs
│   │   │   ├── pageHeroConfig.tsx     # Page hero icon and description mapping
│   │   │   ├── SearchInput.tsx        # Styled search input with clear button
│   │   │   ├── StatCard.tsx           # Metric stat card with trend indicator
│   │   │   ├── StatsGrid.tsx          # Responsive grid for metric cards
│   │   │   ├── StatusBadge.tsx        # Semantic pill badge for active/inactive states
│   │   │   └── TableToolbar.tsx       # Table search, filter, and action toolbar
│   │   ├── ui/              # Radix UI and Tailwind component primitives
│   │   │   ├── badge.tsx              # Variant badge component
│   │   │   ├── button.tsx             # Variant button component with slot support
│   │   │   ├── dialog.tsx             # Accessible dialog primitive
│   │   │   ├── dropdown-menu.tsx      # Accessible dropdown menu primitive
│   │   │   ├── label.tsx              # Accessible form label primitive
│   │   │   ├── select.tsx             # Accessible select dropdown primitive
│   │   │   └── spinner.tsx            # Animated spinner primitive
│   │   ├── international license/     # International license workflows
│   │   │   ├── InternationalLicensesPage.tsx
│   │   │   ├── InternationalLicenseTable.tsx
│   │   │   ├── IssueInternationalLicensePage.tsx
│   │   │   └── international-licenses.css
│   │   ├── local applications/        # Local driving license application workflows
│   │   │   ├── LocalApplicationsPage.tsx
│   │   │   ├── LocalApplicationsList.tsx
│   │   │   ├── LocalApplicationForm.tsx
│   │   │   └── local-applications.css
│   │   ├── AddPersonModal.tsx         # Add new person modal dialog
│   │   ├── ApplicationTypesPage.tsx   # Application types & fee editing modal
│   │   ├── AppRouter.tsx              # Page-level route switcher with RBAC guards
│   │   ├── CountriesPage.tsx          # Country registry directory
│   │   ├── DashboardPage.tsx          # Metrics dashboard & people overview
│   │   ├── DriversPage.tsx            # Drivers directory page
│   │   ├── LicenseClassesPage.tsx     # License classes directory page
│   │   ├── LicensesPage.tsx           # Licenses directory page
│   │   ├── Login.css                  # Light-theme login page stylesheet
│   │   ├── Login.tsx                  # Light-theme institutional login component
│   │   ├── PeoplePage.tsx             # Full people directory page
│   │   ├── PeopleTable.tsx            # Core people data table
│   │   ├── Sidebar.tsx                # Main navigation sidebar with user profile
│   │   ├── TestTypesPage.tsx          # Test types & fee editing modal
│   │   └── Topbar.tsx                 # Top navigation bar with command palette
│   ├── services/            # HTTP service modules calling DVLD.Api
│   │   ├── apiClient.ts               # Unified transport client with 401 interception
│   │   ├── applicationTypeService.ts
│   │   ├── authService.ts
│   │   ├── countryService.ts
│   │   ├── dashboardService.ts
│   │   ├── driverService.ts
│   │   ├── internationalLicenseService.ts
│   │   ├── licenseClassService.ts
│   │   ├── licenseService.ts
│   │   ├── localDrivingLicenseApplicationService.ts
│   │   ├── peopleService.ts
│   │   ├── personService.ts
│   │   └── testTypeService.ts
│   ├── types/               # TypeScript domain models and DTO interfaces
│   ├── utils/               # Formatting, permissions, and storage helpers
│   ├── App.css              # Global application layout and utility styles
│   ├── App.tsx              # Authenticated application shell coordinator
│   ├── index.css            # Base Tailwind v4 configuration and design tokens
│   └── main.tsx             # Application bootstrap (StrictMode + AuthProvider)
├── docs/                    # Authoritative technical documentation
├── components.json          # Shadcn component configuration
├── eslint.config.js         # Flat ESLint configuration
├── index.html               # Main HTML entry shell
├── package.json             # Dependencies and build scripts
├── tsconfig.json            # Solution-style TypeScript configuration
├── tsconfig.app.json        # Application TypeScript compiler settings
├── tsconfig.node.json       # Tooling TypeScript compiler settings
└── vite.config.ts           # Vite bundler configuration
```

---

## 2. Component Hierarchy & Navigation Routing

```text
main.tsx
  └── AuthProvider (context/AuthContext.tsx)
        └── App.tsx
              ├── Login.tsx (rendered when !isAuthenticated)
              └── Authenticated Shell (rendered when isAuthenticated)
                    ├── Sidebar.tsx (navigation links, settings submenus, quick sign out)
                    ├── CommandPalette.tsx (Ctrl+K global action and navigation overlay)
                    ├── Topbar.tsx (active page breadcrumb, search trigger, user dropdown)
                    ├── AppRouter.tsx (guarded by isPagePermitted())
                    │     ├── AccessDenied.tsx (rendered if user role lacks permission)
                    │     ├── DashboardPage.tsx
                    │     ├── PeoplePage.tsx
                    │     ├── DriversPage.tsx
                    │     ├── LicensesPage.tsx
                    │     ├── InternationalLicensesPage.tsx
                    │     ├── IssueInternationalLicensePage.tsx
                    │     ├── LocalApplicationsPage.tsx
                    │     ├── CountriesPage.tsx
                    │     ├── ApplicationTypesPage.tsx
                    │     ├── LicenseClassesPage.tsx
                    │     └── TestTypesPage.tsx
                    ├── AddPersonModal.tsx (controlled modal dialog)
                    └── ConfirmModal.tsx (logout confirmation dialog)
```

---

## 3. State Management & Data Lifecycle

1. **Authentication State (`AuthContext.tsx`):**
   - Centralized authentication state: `accessToken`, `user` profile (`userId`, `userName`, `personId`, `fullName`, `role`), and session expiration.
   - Synchronized with `sessionStorage` (`dvld_auth_session`).
   - Automated cleanup and redirection on session expiry or 401 interception.
2. **Page Navigation State (`App.tsx`):**
   - Active view managed via `activePage: string` (`'Dashboard'`, `'People'`, `'Drivers'`, etc.).
   - Role-based permissions enforced before view rendering in `AppRouter.tsx`.
3. **Data Fetching & Hooks (`src/hooks/`):**
   - Components leverage custom hooks (`usePeople`, `useDashboardStatistics`, `useAsyncData`) to manage request lifecycles, caching, and error states.
   - Pagination, search filtering, and debouncing are abstracted via `usePagination`, `useTableFilters`, and `useDebounce`.
