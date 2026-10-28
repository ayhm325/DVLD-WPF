# ClientApp Feature Status & Known Observations

This document records the verified implementation status of every view, component, and workflow in `ClientApp` as of commit `4f75658f31d634871d9a481a5623519e9dea83e0`.

Status classifications:
- **`Implemented`**: End-to-end UI and API integration are present and functional.
- **`Partially Implemented`**: UI or API integration exists in part, with specific hardcoded values, missing persistence, or incomplete display.
- **`Not Implemented` (`Placeholder`)**: UI is a static placeholder or unrendered route with no backend integration.

---

## 1. Feature Status Matrix

| Area / Feature | Classification | Source Evidence | Details |
|---|---|---|---|
| **Authentication (Login)** | `Implemented` | `src/components/Login.tsx:9-72`, `src/services/authService.ts:22-38` | Authenticates against `POST /api/Auth/login` and stores the returned JWT `accessToken` in `App.tsx` state. |
| **People Directory & Search** | `Implemented` | `src/components/PeopleTable.tsx:11-85`, `src/services/peopleService.ts:3-16`, `src/App.tsx:101-114` | Fetches `GET /api/People`, renders the 5-column table (`ID`, `Full Name`, `National No.`, `DATE OF BIRTH`, `Gender`), and filters rows client-side by `fullName` or `nationalNo`. |
| **Add Person Modal** | `Partially Implemented` | `src/components/AddPersonModal.tsx:16-429`, `src/services/personService.ts:23-71`, `src/services/countryService.ts:9-25` | Loads countries from `GET /api/Countries`, validates inputs, and creates a person via `POST /api/People`. However, selected photos (`imageFile` / `imagePreview`, lines 64–99) are used for local preview only; `createPerson` is called with `imagePath: null` (`line 181`). |
| **Dashboard View** | `Partially Implemented` | `src/App.tsx:158-246` | Displays live counts for `Total People` (`people.length`) and `Total Drivers` (`drivers.length`) and renders a 5-row `People Overview` table. The `Licenses` (`lines 207-215`) and `Applications` (`lines 218-226`) cards hardcode `0` with subtitle `"Connect API to load data"` and do not call `GET /api/Dashboard/statistics`. |
| **Drivers View** | `Partially Implemented` | `src/App.tsx:272-290`, `src/services/driverService.ts:14-30` | Fetches `GET /api/Drivers`, but renders a `.placeholder-panel` showing only `"Total drivers: {drivers.length}"` instead of a drivers table. |
| **Settings — Application Types** | `Not Implemented` (`Placeholder`) | `src/App.tsx:293-333` | Renders a static `.placeholder-panel` (`"Configure and review application types and fees."`) with a `"Back to Dashboard"` button. Does not call `/api/ApplicationTypes`. |
| **Settings — License Classes** | `Not Implemented` (`Placeholder`) | `src/App.tsx:293-333` | Renders a static `.placeholder-panel` (`"Manage driving license classes, minimum age, validity, and fees."`). Does not call `/api/LicenseClasses`. |
| **Settings — Test Types** | `Not Implemented` (`Placeholder`) | `src/App.tsx:293-333` | Renders a static `.placeholder-panel` (`"Manage vision, written, and street test types and fees."`). Does not call `/api/TestTypes`. |
| **Settings — Countries** | `Not Implemented` (`Placeholder`) | `src/App.tsx:293-333` | Renders a static `.placeholder-panel` (`"Browse supported countries used in person records."`). |
| **Licenses Page** | `Not Implemented` (`Placeholder`) | `src/components/Sidebar.tsx:62`, `src/App.tsx:336-343` | Present in `Sidebar.tsx`, but has no render block in `App.tsx` and is excluded from the fallback placeholder condition (`App.tsx:336-343`), producing an empty content area. |
| **Applications Page** | `Not Implemented` (`Placeholder`) | `src/components/Sidebar.tsx:63`, `src/App.tsx:336-343` | Present in `Sidebar.tsx`, but has no render block in `App.tsx` and is excluded from the fallback placeholder condition (`App.tsx:336-343`), producing an empty content area. |
| **Sidebar & Topbar User Profile Info** | `Not Implemented` (`Placeholder`) | `src/components/Sidebar.tsx:95-102`, `src/components/Topbar.tsx:20` | `Sidebar.tsx` hardcodes `"Administrator"` / `"System Admin"` and avatar `"AD"`; `Topbar.tsx` hardcodes avatar `"A"`. |
| **Sign Out Action** | `Not Implemented` | `src/App.tsx`, `src/components/Sidebar.tsx` | No sign-out button is currently rendered to clear `accessToken` without refreshing the browser page. |

---

## 2. Current Technical Observations

These items are documented as current code observations (without modifying application behavior in Phase 3):

1. **Blank Content Area on `Licenses` and `Applications` (`src/App.tsx:336-343`):**
   - `App.tsx` lines 336–343 guard the generic fallback placeholder with:
     ```tsx
     {![
       'Dashboard',
       'People',
       'Drivers',
       'Licenses',
       'Applications',
       ...settingsPages,
     ].includes(activePage) && ( ... )}
     ```
   - Because `'Licenses'` and `'Applications'` are included in that exclusion array while no `{activePage === 'Licenses' && ...}` or `{activePage === 'Applications' && ...}` block exists, selecting either item in `Sidebar.tsx` renders an empty `<main className="content">` element.
2. **Client-Side Validation Rules in `AddPersonModal.tsx` (`lines 142-152`):**
   - `AddPersonModal.tsx` requires `nationalNo` to match `/^\d{10}$/` (exactly 10 digits) and `phone` to match `/^(077|078|079)\d{7}$/` (10-digit Jordanian mobile format).
   - These client-side checks are stricter than the backend `PersonValidator` (`Application/Validators/PersonValidator.cs`), which allows any non-empty `NationalNo` (`<= 20` chars) and `Phone` (`<= 20` chars).
3. **ESLint Error on `npm run lint` (`src/components/AddPersonModal.tsx:67:7`):**
   - Running `npm run lint` reports 1 error under `react-hooks/set-state-in-effect`:
     ```text
     src/components/AddPersonModal.tsx:67:7 error Error: Calling setState synchronously within an effect can trigger cascading renders (react-hooks/set-state-in-effect)
     ```
4. **Role/Policy Mismatch for `Admin` Logins on `GET /api/Drivers`:**
   - As documented in [`API-INTEGRATION.md`](API-INTEGRATION.md), `DriversController` requires `StaffOnly` (`Role = Staff`), so logging into `ClientApp` with an `Admin` account results in `HTTP 403` when `App.tsx` fetches `getDrivers(accessToken)`.
