import type { AuthSession } from '../types/Auth'

export const AUTH_STORAGE_KEY = 'dvld_auth_session'

/**
 * Validates a raw stored session string.
 * Enforces strict checks:
 * - Must be valid JSON matching AuthSession shape
 * - accessToken must be non-empty string
 * - user must have valid userId and userName
 * - expiresAtUtc must be a valid, parseable ISO date strictly in the future
 * Returns null and rejects any malformed, missing, or expired session.
 */
export function validateStoredSession(raw: string | null): AuthSession | null {
  if (!raw || typeof raw !== 'string') return null

  try {
    const session = JSON.parse(raw) as AuthSession
    if (!session || typeof session !== 'object') return null
    if (typeof session.accessToken !== 'string' || !session.accessToken.trim()) return null
    if (!session.user || typeof session.user !== 'object') return null
    if (!session.user.userId || !session.user.userName) return null

    // Strict validation of expiresAtUtc
    if (typeof session.expiresAtUtc !== 'string' || !session.expiresAtUtc.trim()) {
      return null
    }

    const expiresAt = new Date(session.expiresAtUtc).getTime()
    // Non-finite (e.g. NaN from malformed string) or past/present dates are strictly invalid
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      return null
    }

    return session
  } catch {
    return null
  }
}

export function loadStoredSession(): AuthSession | null {
  try {
    const raw = sessionStorage.getItem(AUTH_STORAGE_KEY)
    if (!raw) return null

    const valid = validateStoredSession(raw)
    if (!valid) {
      // Discard invalid, malformed, or expired session from storage
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
      return null
    }

    return valid
  } catch {
    try {
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
    } catch {
      // Ignore storage access errors
    }
    return null
  }
}

export function persistSession(session: AuthSession | null): void {
  try {
    if (session) {
      sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
    } else {
      sessionStorage.removeItem(AUTH_STORAGE_KEY)
    }
  } catch {
    // Ignore storage quota or access errors in restricted environments
  }
}
