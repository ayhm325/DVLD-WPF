import type {
  InternationalLicense,
  InternationalLicenseDetails,
  LocalLicenseInfo,
} from '../types/InternationalLicense'

import { apiRequest } from './apiClient'

export const getInternationalLicenses = (token: string): Promise<InternationalLicense[]> =>
  apiRequest<InternationalLicense[]>('/InternationalLicenses', token)

export const getInternationalLicenseById = (
  token: string,
  id: number,
): Promise<InternationalLicenseDetails> =>
  apiRequest<InternationalLicenseDetails>(`/InternationalLicenses/${id}`, token)

export const getLocalLicenseInfo = (
  token: string,
  localLicenseId: number,
): Promise<LocalLicenseInfo> =>
  apiRequest<LocalLicenseInfo>(
    `/InternationalLicenses/license/${localLicenseId}/info`,
    token,
  )

export const issueInternationalLicense = (
  token: string,
  localLicenseId: number,
): Promise<InternationalLicenseDetails> =>
  apiRequest<InternationalLicenseDetails>('/InternationalLicenses', token, {
    method: 'POST',
    body: JSON.stringify({ localLicenseId }),
  })
