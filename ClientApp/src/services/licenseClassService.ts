import { apiRequest } from './apiClient'

export type LicenseClass = {
  licenseClassId: number
  licenseClassName: string
  licenseClassDescription: string
  minAllowedAge: number
  defaultValidityLength: number
  licenseClassFees: number
}

export function getLicenseClasses(accessToken: string): Promise<LicenseClass[]> {
  return apiRequest<LicenseClass[]>('/LicenseClasses', accessToken)
}
