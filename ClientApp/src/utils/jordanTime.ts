/**
 * Authoritative Jordan Facility Time Utilities
 * Timezone: Asia/Amman
 * Working days: Sunday through Thursday (closed Friday & Saturday)
 * Working hours: 08:00 - 16:00
 * Approved slots: 12 daily 40-minute slots starting from 08:00 to 15:20
 */

export const JORDAN_TIMEZONE = 'Asia/Amman'

export const APPROVED_APPOINTMENT_SLOTS = [
  '08:00',
  '08:40',
  '09:20',
  '10:00',
  '10:40',
  '11:20',
  '12:00',
  '12:40',
  '13:20',
  '14:00',
  '14:40',
  '15:20',
] as const

export type ApprovedAppointmentSlot = (typeof APPROVED_APPOINTMENT_SLOTS)[number]

export interface JordanDateParts {
  year: number
  month: number // 1-12
  day: number // 1-31
  hour: number // 0-23
  minute: number // 0-59
  second: number // 0-59
  dayOfWeek: number // 0 (Sun) to 6 (Sat)
}

const WEEKDAY_MAP: Record<string, number> = {
  Sun: 0,
  Mon: 1,
  Tue: 2,
  Wed: 3,
  Thu: 4,
  Fri: 5,
  Sat: 6,
}

/**
 * Extracts current or specified calendar date/time parts in the authoritative Jordan timezone.
 * Immune to browser or operating system timezone differences.
 */
export function getJordanDateParts(date: Date = new Date()): JordanDateParts {
  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone: JORDAN_TIMEZONE,
    year: 'numeric',
    month: 'numeric',
    day: 'numeric',
    hour: 'numeric',
    minute: 'numeric',
    second: 'numeric',
    weekday: 'short',
    hour12: false,
  })

  const parts = Object.fromEntries(
    formatter.formatToParts(date).map((p) => [p.type, p.value]),
  )

  return {
    year: Number(parts.year),
    month: Number(parts.month),
    day: Number(parts.day),
    hour: Number(parts.hour),
    minute: Number(parts.minute),
    second: Number(parts.second),
    dayOfWeek: WEEKDAY_MAP[parts.weekday] ?? 0,
  }
}

/**
 * Calculates the default appointment date & time for HTML datetime-local input.
 * Default appointment date is the next Jordan facility working day (Sun-Thu) at 08:00 (Slot 01).
 * Never converts to UTC; returns pure wall-clock "YYYY-MM-DDTHH:mm".
 */
export function getDefaultJordanAppointmentDate(fromDate: Date = new Date()): string {
  const jordanToday = getJordanDateParts(fromDate)

  // Working days: Sunday (0) to Thursday (4). Closed: Friday (5) & Saturday (6).
  // Next working day calculation:
  // Thursday (4) -> Sunday (+3)
  // Friday (5) -> Sunday (+2)
  // Saturday (6) -> Sunday (+1)
  // Sun-Wed (0-3) -> Tomorrow (+1)
  let daysToAdd = 1
  if (jordanToday.dayOfWeek === 4) {
    daysToAdd = 3
  } else if (jordanToday.dayOfWeek === 5) {
    daysToAdd = 2
  }

  // Safe calendar addition using UTC to prevent any local browser DST or timezone shift
  const nextWorkingDateUtc = new Date(
    Date.UTC(jordanToday.year, jordanToday.month - 1, jordanToday.day + daysToAdd),
  )

  const yyyy = nextWorkingDateUtc.getUTCFullYear()
  const mm = String(nextWorkingDateUtc.getUTCMonth() + 1).padStart(2, '0')
  const dd = String(nextWorkingDateUtc.getUTCDate()).padStart(2, '0')

  const defaultSlot: ApprovedAppointmentSlot = APPROVED_APPOINTMENT_SLOTS[0] // '08:00'

  return `${yyyy}-${mm}-${dd}T${defaultSlot}`
}

/**
 * Formats appointment datetime for API submission.
 * Sends pure Jordan facility wall-clock timestamp without offset (e.g. "2026-11-10T08:00:00").
 * Preserves exact date, hour, minute and appends :00 seconds without 'Z' or browser offset.
 */
export function formatAppointmentSubmission(dateTimeString: string): string {
  if (!dateTimeString) return ''
  const trimmed = dateTimeString.trim()
  if (trimmed.length === 16) {
    return `${trimmed}:00`
  }
  return trimmed
}

/**
 * Detects whether a date string contains an explicit timezone designator ('Z' or offset like +03:00 / -04:00).
 * Protects against hyphens in date portion (e.g. 2026-11-10) by inspecting characters only after 'T' or space.
 */
export function hasExplicitTimezoneOffset(dateString: string): boolean {
  if (!dateString) return false
  const trimmed = dateString.trim()
  if (trimmed.endsWith('Z') || trimmed.endsWith('z')) {
    return true
  }

  const separatorIndex = trimmed.search(/[Tt\s]/)
  if (separatorIndex >= 0) {
    const timePart = trimmed.substring(separatorIndex)
    return timePart.includes('+') || timePart.includes('-')
  }

  return trimmed.includes('+')
}

/**
 * Formats appointment date & time for table and details display in Jordan facility time.
 * If backend provided pre-formatted string (appointmentDateFormatted), uses it directly.
 * Otherwise handles both timezone-free wall-clock strings and explicit instant timestamps safely.
 */
export function formatJordanAppointmentDateTime(
  rawDate?: string | null,
  formattedFallback?: string | null,
): string {
  if (formattedFallback && formattedFallback.trim()) {
    return formattedFallback.trim()
  }
  if (!rawDate) return '—'

  const trimmed = rawDate.trim()
  if (!trimmed) return '—'

  // Case B — Explicit UTC or Offset (e.g. 2026-11-10T05:00:00Z, 2026-11-10T01:00:00-04:00)
  // Represents an absolute instant: convert explicitly to Asia/Amman
  if (hasExplicitTimezoneOffset(trimmed)) {
    const date = new Date(trimmed)
    if (Number.isNaN(date.getTime())) return trimmed

    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone: JORDAN_TIMEZONE,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    })

    const parts = Object.fromEntries(
      formatter.formatToParts(date).map((p) => [p.type, p.value]),
    )

    return `${parts.year}-${parts.month}-${parts.day} ${parts.hour}:${parts.minute}`
  }

  // Case A — No Explicit Offset (e.g. 2026-11-10T08:00:00, 2026-11-10T08:00:00.000)
  // Represents Jordan facility wall-clock time directly. Preserve date and hour directly.
  const clean = trimmed.replace('T', ' ')
  if (/^\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}/.test(trimmed)) {
    return clean.slice(0, 16)
  }

  // Case C — Invalid or unrecognized input fallback
  const fallbackDate = new Date(trimmed)
  if (Number.isNaN(fallbackDate.getTime())) return trimmed

  return clean
}

/**
 * Formats a calendar date in Jordan facility time (Asia/Amman) as YYYY-MM-DD.
 * Supports an optional offset in days (safe calendar calculation in UTC).
 */
export function getJordanDateString(date: Date = new Date(), offsetDays = 0): string {
  const parts = getJordanDateParts(date)
  const targetUtc = new Date(
    Date.UTC(parts.year, parts.month - 1, parts.day + offsetDays),
  )
  const yyyy = targetUtc.getUTCFullYear()
  const mm = String(targetUtc.getUTCMonth() + 1).padStart(2, '0')
  const dd = String(targetUtc.getUTCDate()).padStart(2, '0')
  return `${yyyy}-${mm}-${dd}`
}

/**
 * Formats a YYYY-MM-DD date string for display in user-friendly format (e.g. "Wed, Nov 4, 2026").
 * Uses UTC representation to prevent browser timezone shift.
 */
export function formatAvailableDateLabel(dateString: string): string {
  if (!dateString) return ''
  const [y, m, d] = dateString.split('-').map(Number)
  if (!y || !m || !d) return dateString
  const dateObj = new Date(Date.UTC(y, m - 1, d))
  return dateObj.toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'UTC',
  })
}

export function isAppointmentInFuture(appointmentDate?: string | null): boolean {
  if (!appointmentDate) return false
  const nowParts = getJordanDateParts(new Date())
  const currentJordanIso = `${nowParts.year}-${String(nowParts.month).padStart(2, '0')}-${String(nowParts.day).padStart(2, '0')}T${String(nowParts.hour).padStart(2, '0')}:${String(nowParts.minute).padStart(2, '0')}:${String(nowParts.second).padStart(2, '0')}`
  const targetClean = appointmentDate.trim().replace(' ', 'T')
  return targetClean > currentJordanIso
}

/**
 * Checks whether a driving license has expired according to Jordan facility local calendar date.
 * Business rule: A driving license remains valid throughout its expiration date in Jordan local time.
 * It becomes expired only on the following calendar day.
 * Conceptually: Expired = JordanToday > ExpirationDate.Date.
 */
export function isLicenseExpiredInJordan(expirationDateStr?: string | null): boolean {
  if (!expirationDateStr) return false
  const expDate = new Date(expirationDateStr)
  if (isNaN(expDate.getTime())) return false

  const expParts = getJordanDateParts(expDate)
  const nowParts = getJordanDateParts(new Date())

  if (nowParts.year > expParts.year) return true
  if (nowParts.year < expParts.year) return false

  if (nowParts.month > expParts.month) return true
  if (nowParts.month < expParts.month) return false

  return nowParts.day > expParts.day
}


