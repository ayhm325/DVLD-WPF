import { apiRequest } from './apiClient'
import type {
  License,
  LocalLicenseInfo,
  RenewLicenseRequest,
  RenewLicenseResponse,
  ReplaceLicenseRequest,
  ReplaceLicenseResponse,
} from '../types/License'

export type {
  License,
  LocalLicenseInfo,
  RenewLicenseRequest,
  RenewLicenseResponse,
  ReplaceLicenseRequest,
  ReplaceLicenseResponse,
}

export function getLicenses(accessToken: string, isExpired?: boolean): Promise<License[]> {
  const query = isExpired !== undefined ? `?isExpired=${isExpired}` : ''
  return apiRequest<License[]>(`/Licenses${query}`, accessToken)
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

export function getLicenseDetailsByLocalAppId(
  accessToken: string,
  localAppId: number,
): Promise<LocalLicenseInfo> {
  return apiRequest<LocalLicenseInfo>(
    `/Licenses/local-application/${localAppId}/details`,
    accessToken,
  )
}

export function issueFirstLicense(
  accessToken: string,
  payload: { localApplicationId: number; notes?: string | null },
): Promise<{ licenseId: number }> {
  return apiRequest<{ licenseId: number }>(
    '/LicenseIssuance/first-license',
    accessToken,
    {
      method: 'POST',
      body: JSON.stringify(payload),
    },
  )
}

export function renewLicense(
  accessToken: string,
  payload: RenewLicenseRequest,
): Promise<RenewLicenseResponse> {
  return apiRequest<RenewLicenseResponse>('/LicenseRenewal', accessToken, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function replaceLicense(
  accessToken: string,
  payload: ReplaceLicenseRequest,
): Promise<ReplaceLicenseResponse> {
  return apiRequest<ReplaceLicenseResponse>('/LicenseReplacement', accessToken, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
