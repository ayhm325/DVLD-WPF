import type {
  InternationalLicense,
  InternationalLicenseDetails,
  LocalLicenseInfo,
} from '../types/InternationalLicense'

const API_BASE_URL = 'http://localhost:5260/api'

async function request<T>(
  accessToken: string,
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      Authorization: `Bearer ${accessToken}`,
      ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      ...init.headers,
    },
  })

  if (!response.ok) {
    const body = await response.text()
    let message = `Request failed (${response.status})`
    if (body) {
      try {
        const parsed = JSON.parse(body) as { message?: string; title?: string; detail?: string }
        message = parsed.message ?? parsed.detail ?? parsed.title ?? body
      } catch {
        message = body
      }
    }
    throw new Error(message)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const getInternationalLicenses = (token: string) =>
  request<InternationalLicense[]>(token, '/InternationalLicenses')

export const getInternationalLicenseById = (token: string, id: number) =>
  request<InternationalLicenseDetails>(token, `/InternationalLicenses/${id}`)

export const getLocalLicenseInfo = (token: string, localLicenseId: number) =>
  request<LocalLicenseInfo>(token, `/InternationalLicenses/license/${localLicenseId}/info`)

export const issueInternationalLicense = (token: string, localLicenseId: number) =>
  request<InternationalLicenseDetails>(token, '/InternationalLicenses', {
    method: 'POST',
    body: JSON.stringify({ localLicenseId }),
  })
