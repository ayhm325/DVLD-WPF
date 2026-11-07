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

export type ApplicationBasicInfo = {
  applicantPersonId: number
  applicationId: number
  applicationStatus: string
  statusText: string
  paidFees: number
  applicationTypeName: string
  applicantFullName: string
  applicationDate: string
  lastStatusDate: string
  createdByUserName: string
}
