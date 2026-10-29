
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

const API_BASE_URL = 'http://localhost:5260/api'

export async function getLicenses(
  accessToken: string,
): Promise<License[]> {
  const response = await fetch(`${API_BASE_URL}/Licenses`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load licenses (${response.status})`)
  }

  return response.json() as Promise<License[]>
}
