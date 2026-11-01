# ClientApp API Integration & Authentication

This document details how `ClientApp` communicates with `DVLD.Api`, manages authentication tokens, handles errors, and interacts with backend CORS and authorization policies.

---

## 1. Unified HTTP Client (`src/services/apiClient.ts`)

All API communication in `ClientApp` routes through the centralized `apiRequest<T>()` transport client:

- **Base URL Resolution:**
  Reads `import.meta.env.VITE_API_BASE_URL` with a fallback to `'http://localhost:5260/api'`.
- **CORS Configuration (`API/DVLD.Api/Program.cs`):**
  `DVLD.Api` enables the `"ReactApp"` CORS policy allowing requests from `http://localhost:5173` (Vite dev server) with any header and method.
- **Request Headers:**
  Automatically attaches `Accept: application/json` and `Content-Type: application/json` (for request bodies), and injects `Authorization: Bearer <token>` when a token is supplied.
- **Error Normalization (`ApiError`):**
  Parses RFC 7807 `ProblemDetails` (`detail`, `title`, `errors`), structured API responses, and network failure states into strongly-typed `ApiError` instances.
- **Automated 401 Unauthorized Interception:**
  `apiClient.ts` exposes `setUnauthorizedHandler()`. When any authenticated request receives HTTP 401, the registered callback automatically terminates the session and redirects the user to the login screen with a session-expired notice.

---

## 2. Frontend Service Modules & Endpoints Consumed

`ClientApp` provides dedicated service modules organized by domain in `src/services/`:

| Service Module | Function | HTTP Method & Route | Backend Policy | Description |
|---|---|---|---|---|
| `authService.ts` | `login()` | `POST /api/Auth/login` | `[AllowAnonymous]` | Authenticates credentials; returns JWT token, user profile, and expiration. |
| `peopleService.ts` | `getPeople()` | `GET /api/People` | `StaffOrAdmin` | Retrieves full people directory for tables and search. |
| `personService.ts` | `createPerson()` | `POST /api/People` | `StaffOrAdmin` | Submits new person records with input validation. |
| `driverService.ts` | `getDrivers()` | `GET /api/Drivers` | `StaffOnly` | Retrieves registered drivers directory. |
| `countryService.ts` | `getCountries()` | `GET /api/Countries` | `[Authorize]` | Fetches country list for nationality selection. |
| `dashboardService.ts` | `getDashboardStatistics()` | `GET /api/Dashboard/statistics` | `[Authorize]` | Loads unified counts (people, drivers, active licenses, pending apps). |
| `licenseService.ts` | `getLicenses()` | `GET /api/Licenses` | `StaffOnly` | Retrieves local driver licenses register. |
| `licenseClassService.ts` | `getLicenseClasses()` | `GET /api/LicenseClasses` | `[Authorize]` | Retrieves license classes, age rules, and fees. |
| `internationalLicenseService.ts` | `getInternationalLicenses()` | `GET /api/InternationalLicenses` | `StaffOnly` | Lists issued international driving permits. |
| `internationalLicenseService.ts` | `issueInternationalLicense()` | `POST /api/InternationalLicenses` | `StaffOnly` | Validates local license and issues international permit. |
| `localDrivingLicenseApplicationService.ts` | `getLocalApplications()` | `GET /api/LocalDrivingLicenseApplications` | `StaffOnly` | Retrieves local driving license application records. |
| `localDrivingLicenseApplicationService.ts` | `createLocalApplication()` | `POST /api/LocalDrivingLicenseApplications` | `StaffOnly` | Submits new local driving license application. |
| `applicationTypeService.ts` | `getApplicationTypes()` | `GET /api/ApplicationTypes` | `[Authorize]` | Lists application fee categories. |
| `applicationTypeService.ts` | `updateApplicationType()` | `PUT /api/ApplicationTypes/{id}` | `AdminOnly` | Updates application type title and fee. |
| `testTypeService.ts` | `getTestTypes()` | `GET /api/TestTypes` | `[Authorize]` | Lists test types (Vision, Written, Street). |
| `testTypeService.ts` | `updateTestType()` | `PUT /api/TestTypes/{id}` | `AdminOnly` | Updates test type fee and description. |

---

## 3. Authentication & Session Architecture

1. **Authentication Flow (`src/components/Login.tsx`):**
   - Submits credentials via `login({ userName, password })`.
   - On success, invokes `setAuthSession(result)` from `useAuth()`.
   - If Remember Me is enabled, stores the username in `localStorage` under `dvld_remembered_username`.
2. **Session Storage (`src/context/AuthContext.tsx`):**
   - Stores the active session in `sessionStorage` under `dvld_auth_session` (`accessToken`, `expiresAtUtc`, and user profile: `userId`, `userName`, `personId`, `fullName`, `role`).
   - `sessionStorage` guarantees per-tab isolation on shared workstations and automatic clearance when the browser tab closes.
3. **Session Restoration & Validation:**
   - On page reload, `loadStoredSession()` verifies token existence, structure, and expiration (`new Date(expiresAtUtc) > Date.now()`).
   - Expired or malformed tokens are purged immediately.
4. **Logout Workflow:**
   - Clears `sessionStorage`, resets in-memory React state, and shifts the view to `<Login />`.

---

## 4. Role-Based Access Control (RBAC) Integration

Backend policies (`AdminOnly`, `StaffOnly`, `StaffOrAdmin`) are respected through:

1. **Route Guarding (`src/components/AppRouter.tsx`):**
   - Checks `isPagePermitted(activePage, userRole)`.
   - Unauthorized attempts render `<AccessDenied />` with a back-to-dashboard shortcut.
2. **Navigation Filtering (`src/components/Sidebar.tsx`):**
   - Sidebar links and settings submenus are conditionally rendered based on the user's role.
3. **Conditional Data Fetching:**
   - Data hooks (such as `usePeople` and `useDashboardStatistics`) only query endpoints when the user possesses the required authorization.
