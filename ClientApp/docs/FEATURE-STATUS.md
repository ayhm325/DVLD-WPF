# ClientApp Feature Status & Implementation Matrix

This document records the verified implementation status of all views, components, workflows, and authentication features in `ClientApp`.

Status classifications:
- **`Implemented`**: End-to-end UI and API integration are present, verified, and functional.
- **`Partially Implemented`**: UI or API integration exists with specific known limitations.
- **`Not Implemented`**: UI is a placeholder with no backend integration.

---

## 1. Feature Status Matrix

| Area / Feature | Classification | Source Evidence | Details |
|---|---|---|---|
| **Authentication (Login)** | `Implemented` | `src/components/Login.tsx`, `src/services/authService.ts`, `src/context/AuthContext.tsx` | Authenticates against `POST /api/Auth/login`, extracts full user profile (`userId`, `userName`, `personId`, `fullName`, `role`), and persists session in `sessionStorage`. |
| **Authentication (Logout)** | `Implemented` | `src/context/AuthContext.tsx`, `src/components/Topbar.tsx`, `src/components/Sidebar.tsx`, `src/components/common/ConfirmModal.tsx` | Complete client-side logout workflow with Topbar dropdown, Sidebar quick-action, confirmation modal, storage cleanup, in-memory state reset, and immediate redirect to Login. |
| **Session Restoration** | `Implemented` | `src/context/AuthContext.tsx` | On browser refresh, restores session from `sessionStorage` if token is unexpired (`expiresAtUtc > Date.now()`); automatically cleans up expired tokens. |
| **401 Unauthorized Interception** | `Implemented` | `src/services/apiClient.ts:47-53`, `src/context/AuthContext.tsx:68-76` | Central API client triggers automatic logout when an endpoint returns HTTP 401 Unauthorized, displaying a session expiration notice on the login page. |
| **User Profile in Topbar & Sidebar** | `Implemented` | `src/components/Topbar.tsx`, `src/components/Sidebar.tsx` | Topbar and Sidebar display dynamic user avatar initial, user full name, and role badge derived from authentication state. |
| **Dashboard View** | `Implemented` | `src/App.tsx:218-295`, `src/services/dashboardService.ts` | Loads unified statistics from `GET /api/Dashboard/statistics` (`totalPeople`, `totalDrivers`, `activeLicenses`, `pendingApplications`) and renders the People overview table. |
| **People Directory & Search** | `Implemented` | `src/components/PeopleTable.tsx`, `src/services/peopleService.ts` | Fetches `GET /api/People`, renders the 5-column table, and filters rows client-side by `fullName` or `nationalNo`. |
| **Add Person Modal** | `Implemented` | `src/components/AddPersonModal.tsx`, `src/services/peopleService.ts` | Loads country dropdown from `GET /api/Countries`, validates inputs, previews optional photo, and creates a person via `POST /api/People`. |
| **Drivers View** | `Implemented` | `src/components/DriversPage.tsx`, `src/services/driverService.ts` | Fetches `GET /api/Drivers`, renders drivers table with national number, full name, active licenses count, and creation date. |
| **Licenses View** | `Implemented` | `src/components/LicensesPage.tsx`, `src/services/licenseService.ts` | Fetches `GET /api/Licenses`, renders licenses table with license ID, driver ID, class, issue date, expiration date, and active status badge. |
| **International Licenses View** | `Implemented` | `src/components/international license/InternationalLicensesPage.tsx` | Lists international licenses with search filters, status tabs (All/Active/Inactive), and details view. |
| **Issue International License** | `Implemented` | `src/components/international license/IssueInternationalLicensePage.tsx` | Form validating local license eligibility and issuing international license via `POST /api/InternationalLicenses`. |
| **Local Driving License Applications** | `Implemented` | `src/components/local applications/LocalApplicationsPage.tsx` | Paginated applications table with search, status filtering, and creation form for new local license applications. |
| **Settings — Countries** | `Implemented` | `src/components/CountriesPage.tsx`, `src/services/countryService.ts` | Displays all countries with quick search. |
| **Settings — Application Types** | `Implemented` | `src/components/ApplicationTypesPage.tsx`, `src/services/applicationTypeService.ts` | Lists application types with fees and provides modal dialog to edit fees and titles via `PUT /api/ApplicationTypes/{id}`. |
| **Settings — License Classes** | `Implemented` | `src/components/LicenseClassesPage.tsx`, `src/services/licenseClassService.ts` | Lists license classes, minimum ages, validity years, and class fees. |
| **Settings — Test Types** | `Implemented` | `src/components/TestTypesPage.tsx`, `src/services/testTypeService.ts` | Lists test types (Vision, Written, Street) and provides modal dialog to edit fees and descriptions via `PUT /api/TestTypes/{id}`. |

---

## 2. Server-Side Token Revocation Notice

As documented in the backend architecture and API controller contracts:
- The backend `DVLD.Api` issues stateless JWT access tokens and does not currently maintain a distributed token revocation blocklist or database session store.
- Client-side logout clears the stored JWT from `sessionStorage` and component state, preventing the browser from making further authenticated requests.
- When server-side revocation endpoints become available in future backend iterations, `AuthContext.logout()` can easily be updated to invoke the revocation endpoint prior to clearing local storage.
