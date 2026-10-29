export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5260/api').replace(/\/$/, '')

type ApiErrorBody = {
  message?: string
  title?: string
  detail?: string
  errors?: Record<string, string[]>
  error?: string
}

type UnauthorizedHandler = (message: string) => void

let unauthorizedHandler: UnauthorizedHandler | null = null

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  unauthorizedHandler = handler
}

async function readError(response: Response): Promise<string> {
  const fallback = `Request failed (${response.status})`
  const text = await response.text()
  if (!text) return fallback

  try {
    const body = JSON.parse(text) as ApiErrorBody
    const validationMessage = body.errors
      ? Object.values(body.errors).flat().join(' ')
      : ''
    return (
      body.error ||
      body.message ||
      body.detail ||
      validationMessage ||
      body.title ||
      fallback
    )
  } catch {
    return text
  }
}

export async function apiRequest<T>(
  path: string,
  accessToken?: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
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
    throw new Error(
      'Unable to connect to the DVLD API. Check that the API is running and the base URL is correct.',
    )
  }

  if (response.status === 401) {
    const errorMessage = await readError(response)
    if (unauthorizedHandler) {
      unauthorizedHandler(errorMessage || 'Session expired. Please log in again.')
    }
    throw new Error(errorMessage || 'Unauthorized. Please log in again.')
  }

  if (!response.ok) {
    throw new Error(await readError(response))
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
