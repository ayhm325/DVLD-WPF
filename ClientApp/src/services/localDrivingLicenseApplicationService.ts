import type {
  LocalApplicationPerson,
  LocalDrivingLicenseApplicationsPage,
  LocalLicenseClass,
} from '../types/LocalDrivingLicenseApplication'

import { API_BASE_URL } from './apiClient'

async function request<T>(accessToken: string, path: string, init: RequestInit = {}): Promise<T> {
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
    let message = `Request failed (${response.status})`
    try {
      const body = await response.json() as { message?: string; title?: string; detail?: string }
      message = body.message ?? body.detail ?? body.title ?? message
    } catch {
      // Keep the HTTP status message when the response is not JSON.
    }
    throw new Error(message)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export function getLocalDrivingLicenseApplications(
  accessToken: string,
  pageNumber: number,
  pageSize: number,
) {
  return request<LocalDrivingLicenseApplicationsPage>(
    accessToken,
    `/LocalDrivingLicenseApplications?pageNumber=${pageNumber}&pageSize=${pageSize}`,
  )
}

export function getLocalApplicationCreateInfo(accessToken: string) {
  return request<{ applicationFees: number }>(accessToken, '/LocalDrivingLicenseApplications/create-info')
}

export function getLocalLicenseClasses(accessToken: string) {
  return request<LocalLicenseClass[]>(accessToken, '/LicenseClasses')
}

export function getPersonById(accessToken: string, personId: number) {
  return request<LocalApplicationPerson>(accessToken, `/People/${personId}`)
}

export function getPersonByNationalNo(accessToken: string, nationalNo: string) {
  return request<LocalApplicationPerson>(accessToken, `/People/national/${encodeURIComponent(nationalNo)}`)
}

export function createLocalDrivingLicenseApplication(
  accessToken: string,
  payload: { applicantPersonId: number; licenseClassId: number },
) {
  return request<{ localDrivingLicenseApplicationId: number }>(
    accessToken,
    '/LocalDrivingLicenseApplications',
    { method: 'POST', body: JSON.stringify(payload) },
  )
}

export function updateLocalDrivingLicenseApplication(
  accessToken: string,
  id: number,
  payload: { licenseClassId: number },
) {
  return request<void>(accessToken, `/LocalDrivingLicenseApplications/${id}`, {
    method: 'PUT', body: JSON.stringify(payload),
  })
}
