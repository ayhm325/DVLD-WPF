# DVLD Backend — Time Handling & `IJordanClock` Guide

## 1. Why `IJordanClock` Exists

DVLD models official driving-license operations for a Jordan facility (`UTC+03:00` year-round).
Accessing `DateTime.Now`, `DateTime.Today`, `DateTime.UtcNow`, or SQL Server `GETDATE()` directly inside services, validators, mappers, or repositories introduces two classes of defects:

1. **UTC / Jordan Midnight Divergence (`21:00–23:59 UTC` = `00:00–02:59` next calendar day in Jordan):**
   Comparing Jordan business dates against `DateTime.UtcNow` causes a 3-hour window every night where license expiration, 18th-birthday eligibility, dashboard "today" counts, and application timestamps evaluate against yesterday's date.
2. **Host / CI Timezone Sensitivity:**
   `DateTime.Now`, `DateTime.Today`, `TimeZoneInfo.Local`, and SQL `GETDATE()` depend on the OS timezone of the web server, container, database host, or CI runner (e.g., UTC Linux runners vs. `UTC+03:00` Windows developer machines).

To eliminate both failure modes and make all time-dependent behavior deterministic under test, **`IJordanClock` is the single authoritative source of current time across the DVLD Backend.**

---

## 2. The `IJordanClock` Contract

Defined in `Application/Interfaces/IJordanClock.cs` and implemented by `Infrastructure/Services/JordanClock.cs` (registered as a singleton in `API/DVLD.Api/Program.cs`):

| Member | Type | `DateTimeKind` | Purpose |
|---|---|---|---|
| `Now` | `DateTime` | `DateTimeKind.Unspecified` | Current Jordan local wall-clock date and time (`YYYY-MM-DD HH:mm:ss` in Amman). Used for business timestamps and facility slot/cutoff comparisons. |
| `Today` | `DateOnly` | N/A (calendar date) | Current Jordan local calendar date (`DateOnly.FromDateTime(Now)`). Used for calendar-day business rules (license expiration, minimum age, appointment advance window, dashboard upcoming cutoff). |
| `UtcNow` | `DateTime` | `DateTimeKind.Utc` | Current global UTC instant. Reserved strictly for technical timestamps that must remain globally unambiguous (such as JWT `exp` / `ExpiresAtUtc`). |

### Architectural Rules
- `Application` depends **only** on `IJordanClock`, `JordanTimeZoneProvider`, and `LicenseExpirationPolicy` — never on `Infrastructure`.
- `JordanClock` in `Infrastructure/Services/JordanClock.cs` is the **only** class in Backend production code permitted to read `DateTime.UtcNow`.
- `IJordanClock` is a **mandatory** constructor dependency on every service that needs the current date or time; optional `IJordanClock? clock = null` fallbacks and static `GetCurrentJordanTime()` helpers are prohibited.

---

## 3. Jordan Local Time vs. UTC Classification

All DVLD business date/time columns in SQL Server use `datetime2` without timezone offsets. Every workflow follows an explicit semantic classification:

| Operation / Column | Clock Source | Semantics & Rationale |
|---|---|---|
| `Applications.ApplicationDate` & `LastStatusDate` | `IJordanClock.Now` | Jordan business-local lifecycle timestamps (`LastStatusDate >= ApplicationDate` monotonically across creation, completion, and cancellation). |
| `Drivers.CreatedDate` | `IJordanClock.Now` | Jordan business-local driver registration timestamp (serialized alongside `Licenses.IssueDate` without UTC offset markers). *Historical note:* rows created prior to `IJordanClock` standardization were stamped with `DateTime.UtcNow`; existing rows are preserved untouched without silent rewrites. |
| `Licenses.IssueDate` & `ExpirationDate` (First-Time, Renewal, Replacement) | `IJordanClock.Now` / `IJordanClock.Today` | Issued and evaluated in Jordan local time. |
| `InternationalLicenses.IssueDate` & `ExpirationDate` | `IJordanClock.Now` / `IJordanClock.Today` | Issued for 1 year in Jordan local time and validated against Jordan calendar dates. |
| `DetainedLicenses.DetainDate` & `ReleaseDate` | `IJordanClock.Now` / `IJordanClock.Today` | Detention timestamps and release reactivation eligibility use Jordan local time. |
| `People.DateOfBirth` (18+ validation & future-date check) | `IJordanClock.Today` | Evaluated by calendar date in Jordan (`PersonValidator.Validate(dto, _clock.Today)`). |
| `TestAppointments.AppointmentDate`, `CancelledAt`, `NoShowDate` | `IJordanClock.Now` / `IJordanClock.Today` | Facility working slots (`08:00–15:20`), 2-calendar-day advance/cancellation cutoffs, exam arrival checks, and 40-minute no-show grace periods operate in Jordan facility time. |
| Dashboard `UpcomingTests` | `IJordanClock.Today` | Parameterized EF Core query (`AppointmentDate >= jordanToday 00:00:00`) rather than SQL Server `GETDATE()`. |
| `JwtTokenResult.ExpiresAtUtc` (`JwtTokenService`) | `IJordanClock.UtcNow` | Technical authentication token expiry (`exp` claim) required by RFC 7519 to be a UTC instant. |

---

## 4. Cross-Platform Timezone Resolution

`Application/Common/Time/JordanTimeZoneProvider.cs` resolves the Jordan timezone once at startup in strict priority order:

1. `"Jordan Standard Time"` (Windows registry timezone ID).
2. `"Asia/Amman"` (Linux / macOS IANA `tzdata` ID).
3. Explicit fixed custom `TimeZoneInfo` at `UTC+03:00` (`SupportsDaylightSavingTime = false`) if minimal container images lack OS `tzdata` / ICU packages.

> **Important:** Jordan permanently abolished Daylight Saving Time in October 2022 and observes `UTC+03:00` year-round. `JordanTimeZoneProvider` **never** falls back to `TimeZoneInfo.Local`.

Incoming API appointment timestamps (`ScheduleTestRequest`, `CreateTestAppointmentRequest`, `UpdateTestAppointmentRequest`) are deserialized through `JordanFacilityDateTimeOffsetConverter`:
- Offsetless ISO strings (e.g., `"2026-11-10T08:00:00"`) represent Jordan facility wall-clock time (`+03:00`).
- Explicit offsets (e.g., `"2026-11-10T05:00:00Z"` or `"2026-11-10T08:00:00+03:00"`) are converted to their exact corresponding Jordan facility wall-clock time (`DateTimeKind.Unspecified`) via `JordanTimeZoneProvider.ConvertUtcToJordan`.

---

## 5. Official DVLD License-Expiration Rule

Centralized in `Application/Common/Time/LicenseExpirationPolicy.cs`:

```csharp
public static bool IsExpired(DateTime expirationDate, DateOnly jordanToday) =>
    jordanToday > DateOnly.FromDateTime(expirationDate);
```

### Canonical Rules
1. **Valid Throughout Expiration Date:** A license remains valid for the **entire** Jordan calendar day of `ExpirationDate.Date` (regardless of any time-of-day component stored on `ExpirationDate`) and becomes expired at **`00:00:00` Jordan time on the following calendar day**.
2. **Identical Repository SQL Semantics:**
   - `LicenseRepository.DeactivateExpiredLicensesAsync(DateOnly jordanToday)` deactivates licenses where `ExpirationDate < jordanToday.ToDateTime(TimeOnly.MinValue)`.
   - `LicenseRepository.ActivateLicenseAsync(int licenseId, DateOnly jordanToday)` and `InternationalRepository.HasActiveInternationalLicenseAsync(int driverId, DateOnly jordanToday)` require `ExpirationDate >= jordanToday.ToDateTime(TimeOnly.MinValue)`.
3. **Validation Precedence (`Expired` Before `!IsActive`):**
   Because background synchronization (`Program.cs` startup and `NoShowBackgroundService`) flips `IsActive = false` once a license expires, workflows that reject expired licenses (`DetainedLicenseService.AddAsync`, `LicenseReplacementService.ReplaceLicenseAsync`, `InternationalService.IssueInternationalLicenseAsync`, and `InternationalService.GetLocalLicenseInfoAsync`) **must check `LicenseExpirationPolicy.IsExpired(...)` before checking `!license.IsActive`** so callers always receive the accurate domain error (`expired` rather than `inactive`).

---

## 6. Injecting `IJordanClock` & Calling Pure Collaborators

- **Application & API Services:** Inject `IJordanClock` as a required constructor parameter and guard with `ArgumentNullException`.
- **Static Validators & Mappers:** Keep validators (`PersonValidator`, `TestAppointmentValidator`) and mappers (`ApplicationMapper`, `DriverMapper`) pure and deterministic by passing `_clock.Today` (`DateOnly`) or `_clock.Now` (`DateTime`) explicitly from the calling service.
- **Repositories:** Do not inject clocks or use SQL `GETDATE()` inside repository queries; pass `DateOnly jordanToday` or `DateTime currentJordanTime` from the Application/Background service into the repository method.

---

## 7. Deterministic Testing with `FakeJordanClock`

Never use `DateTime.Now`, `DateTime.Today`, `DateTime.UtcNow`, or implicit `DateTime -> DateTimeOffset` conversions (`TimeZoneInfo.Local`) in tests.

- **Unit Tests (`Test/Application.UnitTests/Common/FakeJordanClock.cs`):**
  - Construct with a fixed Jordan wall-clock time (`new FakeJordanClock(new DateTime(2026, 11, 3, 10, 0, 0))`) or a fixed UTC instant (`FakeJordanClock.FromUtc(new DateTime(2026, 11, 3, 21, 0, 0, DateTimeKind.Utc))`).
  - Advance time deterministically via `clock.Advance(TimeSpan.FromSeconds(1))` to test exact midnight transitions (`20:59:59Z` -> `21:00:00Z`), 2-day booking cutoffs, 40-minute no-show windows, and 18th-birthday boundaries.
- **HTTP / Controller Integration Tests:**
  - When constructing `DateTimeOffset` request payloads (`ScheduleTestRequest`, `CreateTestAppointmentRequest`, `UpdateTestAppointmentRequest`) from a Jordan wall-clock `DateTime`, construct `new DateTimeOffset(unspecifiedDate, JordanTimeZoneProvider.TimeZone.GetUtcOffset(unspecifiedDate))` explicitly rather than relying on C#'s implicit `DateTime -> DateTimeOffset` conversion (which uses `TimeZoneInfo.Local`).
