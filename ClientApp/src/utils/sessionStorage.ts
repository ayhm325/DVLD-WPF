/**
 * Legacy token persistence cleanup.
 *
 * Earlier builds stored the access token (and sometimes a refresh token) in
 * localStorage/sessionStorage. AUTH-01 moves both out of web storage, so any
 * residual values are purged on startup.
 */
const LEGACY_KEYS = ['access_token', 'dvld_token', 'dvld_auth_session'] as const

export function purgeLegacyTokenStorage(): void {
  if (typeof window === 'undefined') return

  for (const key of LEGACY_KEYS) {
    try {
      localStorage.removeItem(key)
    } catch {
      // Storage can be unavailable (private mode); nothing to purge.
    }

    try {
      sessionStorage.removeItem(key)
    } catch {
      // Storage can be unavailable (private mode); nothing to purge.
    }
  }
}
