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

export interface LicensePageResponse {
  items: License[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export function getLicenses(accessToken: string, isExpired?: boolean): Promise<License[]> {
  const query = isExpired !== undefined ? `?isExpired=${isExpired}` : ''
  return apiRequest<License[]>(`/Licenses${query}`, accessToken)
}

export function getLicensesPage(
  accessToken: string,
  pageNumber: number,
  pageSize: number,
  searchQuery?: string,
  searchBy?: string,
  statusFilter?: string,
  isExpired?: boolean,
): Promise<LicensePageResponse> {
  const params = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  })

  if (searchQuery && searchQuery.trim()) {
    params.set('searchQuery', searchQuery.trim())
  }
  if (searchBy && searchBy !== 'all') {
    params.set('searchBy', searchBy)
  }
  if (statusFilter && statusFilter !== 'all') {
    params.set('statusFilter', statusFilter)
  }
  if (isExpired !== undefined) {
    params.set('isExpired', String(isExpired))
  }

  return apiRequest<LicensePageResponse>(
    `/Licenses?${params.toString()}`,
    accessToken,
  )
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
