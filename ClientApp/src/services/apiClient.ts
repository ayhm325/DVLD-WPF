export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5260/api').replace(/\/$/, '')

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

/**
 * Normalized RFC 7807 failure. Every non-success response from the API is surfaced
 * as an ApiError (or a more specific subclass) so callers can branch on `status`
 * without re-parsing the response body.
 */
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

function normalizeFieldErrors(
  errors: ApiErrorBody['errors'],
): ApiFieldErrors {
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

/**
 * Reads an RFC 7807 ProblemDetails payload (with a fallback for any legacy or
 * non-JSON response) into a normalized ApiError.
 */
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
    if (unauthorizedHandler) {
      unauthorizedHandler(apiError.message)
    }

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

export async function apiRequest<T>(
  path: string,
  accessToken?: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  const effectiveToken =
    accessToken ||
    (typeof window !== 'undefined'
      ? localStorage.getItem('access_token') ||
        localStorage.getItem('dvld_token') ||
        (() => {
          try {
            const raw = sessionStorage.getItem('dvld_auth_session')
            return raw ? (JSON.parse(raw) as { accessToken?: string }).accessToken : undefined
          } catch {
            return undefined
          }
        })()
      : undefined)

  if (effectiveToken) {
    headers.set('Authorization', `Bearer ${effectiveToken}`)
  }

  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  let response: Response

  try {
    response = await fetch(`${API_BASE_URL}${normalizedPath}`, {
      ...init,
      headers,
    })
  } catch {
    throw new ApiError(
      0,
      'Network error',
      'Unable to connect to the DVLD API. Check that the API is running and the base URL is correct.',
    )
  }

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
  const headers = new Headers(init.headers)
  const effectiveToken =
    accessToken ||
    (typeof window !== 'undefined'
      ? localStorage.getItem('access_token') ||
        localStorage.getItem('dvld_token') ||
        (() => {
          try {
            const raw = sessionStorage.getItem('dvld_auth_session')
            return raw ? (JSON.parse(raw) as { accessToken?: string }).accessToken : undefined
          } catch {
            return undefined
          }
        })()
      : undefined)

  if (effectiveToken) {
    headers.set('Authorization', `Bearer ${effectiveToken}`)
  }

  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  let response: Response

  try {
    response = await fetch(`${API_BASE_URL}${normalizedPath}`, {
      ...init,
      headers,
    })
  } catch {
    throw new ApiError(
      0,
      'Network error',
      'Unable to connect to the DVLD API. Check that the API is running and the base URL is correct.',
    )
  }

  if (!response.ok) {
    await throwApiError(response)
  }

  return await response.blob()
}
