
export type LicenseClass = {
  licenseClassId: number
  licenseClassName: string
  licenseClassDescription: string
  minAllowedAge: number
  defaultValidityLength: number
  licenseClassFees: number
}

const API_BASE_URL = 'http://localhost:5260/api'

export async function getLicenseClasses(
  accessToken: string,
): Promise<LicenseClass[]> {
  const response = await fetch(`${API_BASE_URL}/LicenseClasses`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load license classes (${response.status})`)
  }

  return response.json() as Promise<LicenseClass[]>
}
