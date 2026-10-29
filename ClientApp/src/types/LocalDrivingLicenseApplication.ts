export type LocalDrivingLicenseApplication = {
  localDrivingLicenseApplicationId: number
  licenseClassId: number
  licenseClassName: string | null
  nationalNo: string | null
  fullName: string | null
  applicationDate: string
  passedTest: number
  applicationStatus: string
  statusText: string
  applicationFees: number
  licenseClassFees: number
  hasLicense: boolean
  applicantPersonId: number
}

export type LocalDrivingLicenseApplicationsPage = {
  items: LocalDrivingLicenseApplication[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export type LocalApplicationPerson = {
  personId: number
  nationalNo: string
  fullName: string
  dateOfBirth: string
  gender: number
  address: string
  phone: string
  email: string | null
  countryName: string
}

export type LocalLicenseClass = {
  licenseClassId: number
  licenseClassName: string
  licenseClassFees: number
  licenseClassDescription?: string
  minAllowedAge?: number
  defaultValidityLength?: number
}
