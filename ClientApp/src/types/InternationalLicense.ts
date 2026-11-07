export type InternationalLicense = {
  internationalLicenseId: number
  applicationId: number
  driverId: number
  issuedUsingLocalLicenseId: number
  personId: number
  issueDate: string
  expirationDate: string
  isActive: boolean
}

export type InternationalLicenseDetails = {
  internationalLicenseId: number
  applicationId: number
  driverId: number
  issuedUsingLocalLicenseId: number
  issueDate: string
  expirationDate: string
  isActive: boolean
  createdByUserId: number
  personId: number
  fullName: string
  dateOfBirth: string
  imagePath: string
  nationalNo: string
  gender: string
  fees: number
  createdByUserName: string
}
