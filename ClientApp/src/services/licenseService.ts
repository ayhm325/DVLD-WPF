import { apiRequest } from './apiClient'
import type { License } from '../types/License'
import type { LocalLicenseInfo } from '../types/InternationalLicense'

export type { License, LocalLicenseInfo }

export function getLicenses(accessToken: string): Promise<License[]> {
  return apiRequest<License[]>('/Licenses', accessToken)
}

export function getLicensesByDriverId(accessToken: string, driverId: number): Promise<License[]> {
  return apiRequest<License[]>(`/Licenses/driver/${driverId}`, accessToken)
}

export function getLicensesByPersonId(accessToken: string, personId: number): Promise<License[]> {
  return apiRequest<License[]>(`/Licenses/person/${personId}`, accessToken)
}

export function getLicenseDetailsById(
  accessToken: string,
  licenseId: number,
): Promise<LocalLicenseInfo> {
  return apiRequest<LocalLicenseInfo>(`/Licenses/${licenseId}/details`, accessToken)
}
