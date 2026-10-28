# ClientApp API Integration & Authentication

This document details how `ClientApp` communicates with `DVLD.Api`, manages authentication tokens, and interacts with backend CORS and authorization policies.

---

## 1. Base URL and CORS Boundary

- **API Base URL:** All 5 service files in `ClientApp/src/services/*.ts` define:
  ```ts
  const API_BASE_URL = 'http://localhost:5260/api'
  ```
- **CORS Configuration (`API/DVLD.Api/Program.cs:283-292, 374`):**
  `DVLD.Api` registers and enables the `"ReactApp"` CORS policy:
  - Allowed Origin: `http://localhost:5173`
  - Allowed Headers: `*` (`AllowAnyHeader()`)
  - Allowed Methods: `*` (`AllowAnyMethod()`)
- **Implication:** `ClientApp` must run on `http://localhost:5173` (Vite's default port) and `DVLD.Api` must run on `http://localhost:5260` (the `http` launch profile in `API/DVLD.Api/Properties/launchSettings.json`).

---

## 2. Frontend Service Modules and Endpoints Consumed

Currently, `ClientApp` consumes **4 out of the 101** HTTP endpoints exposed by `DVLD.Api`:

| Service File | Exported Function | HTTP Method & Route | API Authorization Policy | Request / Response Notes |
|---|---|---|---|---|
| `src/services/authService.ts` | `login(request)` | `POST /api/Auth/login` | `[AllowAnonymous]` + `LoginRateLimit` (5 req/min/IP) | Sends `{ userName, password }`. Returns `LoginResponse` (`accessToken`, `expiresAtUtc`, `userId`, `userName`, `personId`, `fullName`, `role`). |
| `src/services/peopleService.ts` | `getPeople(accessToken)` | `GET /api/People` | `StaffOrAdmin` (`Staff`, `Admin`) | Sends `Authorization: Bearer <accessToken>`. Returns array of people mapped to `Person` (`src/types/Person.ts`). |
| `src/services/personService.ts` | `createPerson(accessToken, person)` | `POST /api/People` | `StaffOrAdmin` (`Staff`, `Admin`) | Sends `CreatePersonRequest` JSON body. Parses `ProblemDetails` (`detail` or `title`) on non-2xx responses. Returns `{ personId }`. |
| `src/services/driverService.ts` | `getDrivers(accessToken)` | `GET /api/Drivers` | `StaffOnly` (`Staff` only) | Sends `Authorization: Bearer <accessToken>`. Returns `Driver[]`. |
| `src/services/countryService.ts` | `getCountries(accessToken)` | `GET /api/Countries` | `[Authorize]` (any active user) | Sends `Authorization: Bearer <accessToken>`. Returns `Country[]` (`countryId`, `countryName`). |

> **Observation — Split People Services:** Read and write operations for `/api/People` are currently split across two separate files: `src/services/peopleService.ts` (`getPeople`) and `src/services/personService.ts` (`createPerson`).

---

## 3. Authentication Flow & Token Handling

1. **Login Submission (`src/components/Login.tsx:15-28`):**
   - `Login.tsx` calls `login({ userName, password })`.
   - On success, `Login.tsx` passes only `result.accessToken` to `onLoginSuccess(result.accessToken)`. Other fields returned by `POST /api/Auth/login` (`fullName`, `userName`, `role`, `userId`, `personId`, `expiresAtUtc`) are defined in `authService.ts:9-17` but are not currently stored by `App.tsx`.
2. **In-Memory Token Storage (`src/App.tsx:19`):**
   - `App.tsx` stores the JWT in `const [accessToken, setAccessToken] = useState<string | null>(null)`.
   - No tokens are written to `localStorage`, `sessionStorage`, or cookies.
3. **Bearer Header Injection:**
   - Each authenticated service function accepts `accessToken: string` explicitly and sets `Authorization: Bearer ${accessToken}` in the `fetch` request headers.

---

## 4. Interaction with Backend Authorization Policies (`StaffOnly` vs `Admin`)

In `API/DVLD.Api/Program.cs:125-138`, backend authorization policies are defined as:
- `AdminOnly` → requires role `Admin`
- `StaffOnly` → requires role `Staff` (does **not** include `Admin`)
- `StaffOrAdmin` → requires role `Staff` or `Admin`

### Observed Runtime Behavior in `ClientApp`
- When a `Staff` user logs into `ClientApp`, both `GET /api/People` (`StaffOrAdmin`) and `GET /api/Drivers` (`StaffOnly`) succeed (`200 OK`).
- When an `Admin` user logs into `ClientApp`, `App.tsx:67-98` unconditionally executes both `loadPeople()` and `loadDrivers()`:
  - `GET /api/People` (`StaffOrAdmin`) succeeds (`200 OK`).
  - `GET /api/Drivers` (`DriversController.cs:16`, `StaffOnly`) returns **`403 Forbidden`**, causing `App.tsx` to set `driversError` (`"Failed to load drivers (403)"`) on the Dashboard and Drivers views.
