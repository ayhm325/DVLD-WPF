import { apiRequest } from './apiClient'
import type { LicenseClass } from '../types/LicenseClass'

export type { LicenseClass }

export function getLicenseClasses(accessToken: string): Promise<LicenseClass[]> {
  return apiRequest<LicenseClass[]>('/LicenseClasses', accessToken)
}
