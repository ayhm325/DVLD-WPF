import { apiRequest } from './apiClient'
import type {
  LicenseClass,
  UpdateLicenseClassRequest,
} from '../types/LicenseClass'

export type { LicenseClass, UpdateLicenseClassRequest }

export function getLicenseClasses(accessToken: string): Promise<LicenseClass[]> {
  return apiRequest<LicenseClass[]>('/LicenseClasses', accessToken)
}

export function updateLicenseClass(
  accessToken: string,
  id: number,
  request: UpdateLicenseClassRequest,
): Promise<void> {
  return apiRequest<void>(`/LicenseClasses/${id}`, accessToken, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
