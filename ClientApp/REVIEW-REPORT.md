# ClientApp review — commit 7210d9bd19edd8874d01b8ebc4aeb9d8d38ab2e2

## Changes made in this review copy

- Added `src/services/apiClient.ts` as a shared HTTP boundary.
  - Reads `VITE_API_BASE_URL` with a localhost fallback.
  - Adds `Accept`, bearer authorization, and JSON content type when appropriate.
  - Handles empty `204` responses and non-JSON responses.
  - Normalizes API/ProblemDetails/validation errors and gives a useful network error.
- Migrated authentication, people, person creation, drivers, countries, license classes, licenses, application types, and test types to the shared client.
- Centralized the API base URL rather than repeating it across these services.

## Findings still requiring attention

1. **Role-aware loading:** `App.tsx` loads drivers after every login. The API controller uses `StaffOnly`, so an Admin may receive 403. The frontend should respect the current role or the backend policy should intentionally include Admin if that is the business rule.
2. **Hardcoded dashboard cards:** License and application totals are hardcoded to `0`; wire them to the dashboard statistics endpoint or clearly mark them unavailable.
3. **Page architecture:** `App.tsx` remains a large page switcher and owns data fetching, dashboard markup, and modal lifecycle. Split into a small authenticated shell, page components, and shared hooks incrementally.
4. **International license details:** The list accepts `onOpenDetails`, but the parent does not supply a details flow. Add a real details route/page or remove the inactive callback until implemented.
5. **Local applications filtering:** Search/status filtering is client-side over the currently loaded page, not the complete server result. Make the UI explicit or implement server-side filtering and reset the page when filters change.
6. **Photo upload:** Add Person currently previews a selected photo locally but sends `imagePath: null`; the UI must not imply that the photo is saved unless the API supports upload.
7. **Lint and build:** `AddPersonModal.tsx` was previously identified as having a `react-hooks/set-state-in-effect` lint error. Run lint after dependencies install and resolve all reported issues.
8. **Documentation:** `docs/FEATURE-STATUS.md` and `docs/ARCHITECTURE.md` contain stale descriptions of the frontend. Update them to match the current implementation after remaining work.
9. **Configuration and security:** Keep production API URLs in environment configuration; do not place secrets in `VITE_*` variables. If persistence is added for login, review the token-storage threat model first.
10. **UX consistency:** Standardize loading, empty, retry, validation, and success states; consolidate duplicated table/form styling only where components share the same interaction and visual semantics.

## Verification limitation

`npm run build` was attempted in this review environment but could not run because the installed dependency tree is incomplete (`vite/client` and `node` type definitions were missing). `npm ci` did not complete within the available time. Therefore these edits are not claimed to be build-verified.

## Local verification

From `ClientApp` run:

```powershell
npm ci
npm run build
npm run lint
```

Resolve compiler/lint errors before merging. This review copy has not been pushed to GitHub.
