export type License = {
  licenseId: number
  applicationId: number
  driverId: number
  driverName: string | null
  licenseClassId: number
  licenseClassName: string | null
  issueDate: string
  expirationDate: string
  notes: string | null
  paidFees: number
  isActive: boolean
  isExpired?: boolean
  isEligibleForRenewal?: boolean
  ineligibilityReason?: string | null
  issueReason: number
  issueReasonText: string | null
  createdByUserId: number
  createdByUserName: string | null
}

export type RenewLicenseRequest = {
  oldLicenseId: number
  notes?: string | null
}

export type RenewLicenseResponse = {
  licenseId: number
}

export type ReplaceLicenseRequest = {
  oldLicenseId: number
  replacementReason: string
}

export type ReplaceLicenseResponse = {
  licenseId: number
}
