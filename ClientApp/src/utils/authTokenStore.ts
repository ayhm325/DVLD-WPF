/**
 * Access tokens live only in memory. Refresh tokens live only in an HttpOnly
 * cookie and are never readable from JavaScript.
 */
let currentAccessToken: string | null = null

export function getAccessToken(): string | null {
  return currentAccessToken
}

export function setAccessToken(token: string | null): void {
  currentAccessToken = token
}
