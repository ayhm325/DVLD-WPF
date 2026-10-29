# DVLD React Client — Security Architecture & Policy Documentation

## 1. Authentication Storage Architecture

### Session Storage vs. Local Storage
The DVLD React Client stores active authentication tokens exclusively in `sessionStorage` under the key `dvld_auth_session`.
- **Per-Tab Isolation**: In government and departmental offices (such as DVLD branches), workstations are frequently shared across personnel shifts. `sessionStorage` scopes credentials to the single active browser tab/session and is automatically discarded as soon as the tab or browser is closed.
- **Why `localStorage` is Prohibited**: `localStorage` persists data indefinitely across browser restarts, OS reboots, and across all open tabs simultaneously. If an agent walks away from a shared workstation, a token in `localStorage` remains accessible until manual clearance, posing a severe compliance and security vulnerability.

### Client-Side JavaScript and HttpOnly Cookies
- **Technical Reality**: Real `HttpOnly` cookies cannot be set, accessed, or manipulated by client-side JavaScript (`document.cookie` or client APIs). This is by design in browser security standards to mitigate XSS token exfiltration.
- **Backend Architecture Recommendation**: To migrate from Bearer token storage in memory/`sessionStorage` to `HttpOnly` cookies, the backend ASP.NET Core API (`DVLD.Api`) must issue an authentic `Set-Cookie` response header during `/api/Auth/login` with flags:
  ```http
  Set-Cookie: dvld_access_token=<JWT>; Path=/; Secure; HttpOnly; SameSite=Strict; Max-Age=3600
  ```
  And subsequent API requests will automatically transmit the cookie across origins when `credentials: 'include'` is configured.

---

## 2. XSS & Credential Leakage Audit

A comprehensive static analysis and code audit was conducted on the entire `ClientApp/src` codebase:
1. **DOM Injection (XSS)**:
   - `dangerouslySetInnerHTML`: **0 occurrences found.**
   - `innerHTML`, `outerHTML`, `document.write`: **0 occurrences found.**
   - All user-supplied inputs and rendered table cells use React JSX text escaping and typed properties.
2. **Credential Logging**:
   - `console.log`, `console.error`, `console.warn`: **0 occurrences found.**
   - No passwords, tokens, or personal identity numbers are emitted to browser developer tools or network log sinks.
3. **Strict Expiration & Shape Validation**:
   - `validateStoredSession`: Rejects and purges any session where `expiresAtUtc` is missing, unparseable (`NaN`), or less than or equal to `Date.now()`.
   - Active session timeout automatically logs the user out when token lifespan elapses, preventing stale token reuse.

---

## 3. Recommended Content Security Policy (CSP) & Security Headers

For production deployment via reverse proxy (e.g., Nginx, IIS, Cloudflare, or Azure App Service), the following HTTP response headers are recommended:

### Content Security Policy (CSP)
```http
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com data:; img-src 'self' data: https:; connect-src 'self' http://localhost:5260 https://api.dvld.local; frame-ancestors 'none'; base-uri 'self'; form-action 'self';
```

### Supplementary Defensive Headers
```http
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
```

---

## 4. Authoritative Role-Based Access Control (RBAC) Mapping

The frontend routes and navigation items strictly reflect the backend ASP.NET Core authorization policies (`DVLD.Api`):

| Page / Route | Allowed Roles | Backend Policy & Controller |
| :--- | :--- | :--- |
| **Dashboard** | `Admin`, `Staff` | `[Authorize]` (`DashboardController`) |
| **People** | `Admin`, `Staff` | `StaffOrAdmin` (`PeopleController`) |
| **Drivers** | `Staff` only | `StaffOnly` (`DriversController`) |
| **Licenses** | `Staff` only | `StaffOnly` (`LicensesController`) |
| **International Licenses** | `Staff` only | `StaffOnly` (`InternationalLicensesController`) |
| **Local Applications** | `Staff` only | `StaffOnly` (`LocalDrivingLicenseApplicationsController`) |
| **Application Types** | `Admin` only | `AdminOnly` (`ApplicationTypesController`) |
| **License Classes** | `Admin`, `Staff` | `[Authorize]` (`LicenseClassesController`) |
| **Test Types** | `Admin`, `Staff` | `[Authorize]` (`TestTypesController`) |
| **Countries** | `Admin`, `Staff` | `[Authorize]` (`CountriesController`) |

### Enforcement Mechanics:
1. **Sidebar Navigation**: Navigation links and settings submenus are filtered via `isPagePermitted(page, user.role)`. Restricted items are completely hidden from the DOM.
2. **Conditional Data Loading**: API endpoints (e.g. `getDrivers`) are not triggered unconditionally on mount; they are queried only when the user is authorized and visiting the corresponding page.
3. **Route Guard & AccessDenied State**: Direct access or forced route selection renders a dedicated `<AccessDenied />` component explaining permission constraints and offering an immediate return to the Dashboard.
4. **403 Forbidden Response Handling**: Any API call returning HTTP 403 throws a `ForbiddenError` with the message `"You do not have permission to access this page."`, preventing application freeze or half-rendered states.
