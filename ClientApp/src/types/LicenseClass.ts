export type LicenseClass = {
  licenseClassId: number
  licenseClassName: string
  licenseClassDescription: string
  minAllowedAge: number
  defaultValidityLength: number
  licenseClassFees: number
}

export type UpdateLicenseClassRequest = {
  licenseClassId?: number
  licenseClassName: string
  licenseClassDescription: string
  minAllowedAge: number
  defaultValidityLength: number
  licenseClassFees: number
}
