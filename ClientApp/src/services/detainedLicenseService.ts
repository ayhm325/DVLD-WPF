import { apiRequest } from './apiClient'
import type {
  DetainedLicense,
  CreateDetainedLicenseRequest,
  ReleaseDetainedLicenseRequest,
} from '../types/DetainedLicense'

export type { DetainedLicense, CreateDetainedLicenseRequest, ReleaseDetainedLicenseRequest }

export function getDetainedLicenses(accessToken: string): Promise<DetainedLicense[]> {
  return apiRequest<DetainedLicense[]>('/DetainedLicenses', accessToken)
}

export function getDetainedLicenseById(
  accessToken: string,
  id: number,
): Promise<DetainedLicense> {
  return apiRequest<DetainedLicense>(`/DetainedLicenses/${id}`, accessToken)
}

export function getActiveDetainByLicenseId(
  accessToken: string,
  licenseId: number,
): Promise<DetainedLicense> {
  return apiRequest<DetainedLicense>(
    `/DetainedLicenses/license/${licenseId}/active`,
    accessToken,
  )
}

export function checkIsLicenseDetained(
  accessToken: string,
  licenseId: number,
): Promise<{ detained: boolean }> {
  return apiRequest<{ detained: boolean }>(
    `/DetainedLicenses/license/${licenseId}/detained`,
    accessToken,
  )
}

export function detainLicense(
  accessToken: string,
  payload: CreateDetainedLicenseRequest,
): Promise<DetainedLicense> {
  return apiRequest<DetainedLicense>('/DetainedLicenses', accessToken, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function releaseDetainedLicense(
  accessToken: string,
  detainId: number,
): Promise<void> {
  return apiRequest<void>('/DetainedLicenses/release', accessToken, {
    method: 'POST',
    body: JSON.stringify({ detainId }),
  })
}
