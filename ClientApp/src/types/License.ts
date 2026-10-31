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
  issueReason: number
  issueReasonText: string | null
  createdByUserId: number
  createdByUserName: string | null
}
