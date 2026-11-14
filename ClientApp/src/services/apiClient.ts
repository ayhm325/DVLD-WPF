import { getAccessToken, setAccessToken } from '../utils/authTokenStore'
import type { LoginResponse } from '../types/Auth'

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/$/, '')

const CSRF_HEADER = 'X-CSRF-Protection'

export type ApiFieldErrors = Record<string, string[]>

type ApiErrorBody = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  errors?: Record<string, string[] | string>
  message?: string
  error?: string
}

type UnauthorizedHandler = (message: string) => void

export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail: string
  readonly fieldErrors: ApiFieldErrors
  readonly traceId?: string

  constructor(
    status: number,
    title: string,
    detail: string,
    fieldErrors: ApiFieldErrors = {},
    traceId?: string,
  ) {
    super(detail || title || `Request failed (${status})`)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.fieldErrors = fieldErrors
    this.traceId = traceId
  }

  get hasFieldErrors(): boolean {
    return Object.keys(this.fieldErrors).length > 0
  }

  get fieldMessages(): string[] {
    return Object.values(this.fieldErrors).flat()
  }
}

export class UnauthorizedError extends ApiError {
  constructor(
    message = 'Your session has expired. Please log in again.',
    fieldErrors: ApiFieldErrors = {},
    traceId?: string,
  ) {
    super(401, 'Unauthorized', message, fieldErrors, traceId)
    this.name = 'UnauthorizedError'
  }
}

export class ForbiddenError extends ApiError {
  constructor(
    message = 'You do not have permission to access this page.',
    fieldErrors: ApiFieldErrors = {},
    traceId?: string,
  ) {
    super(403, 'Forbidden', message, fieldErrors, traceId)
    this.name = 'ForbiddenError'
  }
}

let unauthorizedHandler: UnauthorizedHandler | null = null

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  unauthorizedHandler = handler
}

function normalizeFieldErrors(errors: ApiErrorBody['errors']): ApiFieldErrors {
  if (!errors) return {}

  const normalized: ApiFieldErrors = {}

  for (const [key, value] of Object.entries(errors)) {
    const messages = (Array.isArray(value) ? value : [value]).filter(
      (message): message is string =>
        typeof message === 'string' && message.trim().length > 0,
    )

    if (messages.length > 0) {
      normalized[key] = messages
    }
  }

  return normalized
}

async function readApiError(response: Response): Promise<ApiError> {
  const fallback = `Request failed (${response.status})`
  const text = await response.text()

  if (!text) {
    return new ApiError(response.status, fallback, fallback)
  }

  let body: ApiErrorBody

  try {
    body = JSON.parse(text) as ApiErrorBody
  } catch {
    return new ApiError(response.status, fallback, text.trim() || fallback)
  }

  const fieldErrors = normalizeFieldErrors(body.errors)
  const detail = typeof body.detail === 'string' ? body.detail.trim() : ''
  const title = typeof body.title === 'string' ? body.title.trim() : ''
  const legacyMessage =
    typeof body.error === 'string'
      ? body.error
      : typeof body.message === 'string'
        ? body.message
        : ''
  const fieldMessage = Object.values(fieldErrors).flat().join(' ')
  const traceId = typeof body.traceId === 'string' ? body.traceId : undefined

  const message = detail || fieldMessage || legacyMessage || title || fallback

  return new ApiError(
    response.status,
    title || fallback,
    message,
    fieldErrors,
    traceId,
  )
}

async function throwApiError(response: Response): Promise<never> {
  const apiError = await readApiError(response)

  if (response.status === 401) {
    throw new UnauthorizedError(
      apiError.message,
      apiError.fieldErrors,
      apiError.traceId,
    )
  }

  if (response.status === 403) {
    throw new ForbiddenError(
      apiError.message,
      apiError.fieldErrors,
      apiError.traceId,
    )
  }

  throw apiError
}

// ---------------------------------------------------------------------------
// Refresh single-flight
//
// Every 401 in this browser context shares one refresh request. A successful
// refresh replaces the in-memory access token; the rotating refresh token is
// handled entirely by the HttpOnly cookie.
// ---------------------------------------------------------------------------

let inFlightRefresh: Promise<LoginResponse | null> | null = null

export function refreshSession(): Promise<LoginResponse | null> {
  if (!inFlightRefresh) {
    inFlightRefresh = performRefresh(0).finally(() => {
      inFlightRefresh = null
    })
  }

  return inFlightRefresh
}

async function performRefresh(attempt: number): Promise<LoginResponse | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/Auth/refresh`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        [CSRF_HEADER]: '1',
      },
    })

    if (response.status === 409 && attempt === 0) {
      // A concurrent tab/request won the rotation. Its Set-Cookie may already be
      // applied; retry once with the current cookie before giving up.
      await delay(250)
      return performRefresh(1)
    }

    if (!response.ok) {
      if (response.status === 401) {
        setAccessToken(null)
      }

      return null
    }

    const session = (await response.json()) as LoginResponse
    setAccessToken(session.accessToken)
    return session
  } catch {
    return null
  }
}

export async function logoutSession(): Promise<void> {
  try {
    await fetch(`${API_BASE_URL}/Auth/logout`, {
      method: 'POST',
      credentials: 'include',
      headers: { [CSRF_HEADER]: '1' },
    })
  } catch {
    // Logout is best-effort locally; the cookie is cleared by the browser only on
    // a successful response, so a failed request leaves the session intact.
  } finally {
    setAccessToken(null)
  }
}

export async function logoutAllSessions(): Promise<void> {
  try {
    await apiRequest<void>('/Auth/logout-all', undefined, {
      method: 'POST',
      headers: { [CSRF_HEADER]: '1' },
    })
  } catch {
    // The local session is cleared regardless of the server response.
  } finally {
    setAccessToken(null)
  }
}

function buildHeaders(init: RequestInit, token?: string): Headers {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  const effectiveToken = token ?? getAccessToken()

  if (effectiveToken) {
    headers.set('Authorization', `Bearer ${effectiveToken}`)
  }

  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  return headers
}

async function send(
  path: string,
  init: RequestInit,
  token?: string,
): Promise<Response> {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`

  try {
    return await fetch(`${API_BASE_URL}${normalizedPath}`, {
      ...init,
      credentials: 'include',
      headers: buildHeaders(init, token),
    })
  } catch {
    throw new ApiError(
      0,
      'Network error',
      'Unable to connect to the DVLD API. Check that the API is running and the base URL is correct.',
    )
  }
}

async function sendWithRefresh(
  path: string,
  init: RequestInit,
  token?: string,
): Promise<Response> {
  let response = await send(path, init, token)

  if (response.status !== 401) {
    return response
  }

  const refreshed = await refreshSession()

  if (!refreshed) {
    unauthorizedHandler?.('Your session has expired. Please log in again.')
    return response
  }

  // Retry the original request exactly once with the new access token.
  response = await send(path, init, refreshed.accessToken)
  return response
}

export async function apiRequest<T>(
  path: string,
  accessToken?: string,
  init: RequestInit = {},
): Promise<T> {
  const response = await sendWithRefresh(path, init, accessToken)

  if (!response.ok) {
    await throwApiError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (!contentType.includes('application/json')) {
    return undefined as T
  }

  return (await response.json()) as T
}

export async function apiBlobRequest(
  path: string,
  accessToken?: string,
  init: RequestInit = {},
): Promise<Blob> {
  const response = await sendWithRefresh(path, init, accessToken)

  if (!response.ok) {
    await throwApiError(response)
  }

  return await response.blob()
}

function delay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, milliseconds)
  })
}
