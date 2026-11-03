export type DetainedLicense = {
  detainId: number
  licenseId: number
  detainDate: string
  fineFees: number
  createdByUserId: number
  isReleased: boolean
  releaseDate: string | null
  releasedByUserId: number | null
  releaseApplicationId: number | null
  nationalNo: string | null
  fullName: string | null
}

export type CreateDetainedLicenseRequest = {
  licenseId: number
  fineFees: number
}

export type ReleaseDetainedLicenseRequest = {
  detainId: number
}
